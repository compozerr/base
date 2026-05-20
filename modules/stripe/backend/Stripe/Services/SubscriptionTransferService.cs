using Microsoft.Extensions.Options;
using Organizations.Abstractions;
using Serilog;
using Stripe.Options;

namespace Stripe.Services;

/// <summary>
/// Migrates Stripe subscriptions between organizations during a project transfer.
///
/// Subscriptions are per-environment (each <c>ProjectEnvironment.StripeSubscriptionId</c>),
/// so a project with N environments can have up to N active subs. Each is migrated
/// independently. The caller (TransferProjectRoute) hands in the list of
/// subscription ids it found on the project's environments, and gets back a list
/// of new subscription ids to write back.
///
/// Billing rules per migrated subscription:
///   - Old was trialing (or has trial_end in the future) → preserve the trial on
///     the new sub so the user is not charged for a period they're not yet paying for.
///   - Old was active/past_due → new sub starts paying immediately, and the old
///     is cancelled with prorate=true so the source org gets credit for unused time.
///
/// Caller is responsible for: (a) authorising the transfer, (b) updating
/// <c>StripeSubscriptionId</c> + <c>BillingStatus</c> on each environment, and
/// (c) flipping <c>Project.OrganizationId</c>. The orchestration rule is: call
/// this BEFORE applying any DB changes. If it throws, no DB writes should happen.
/// </summary>
public sealed class SubscriptionTransferService(
    IOptions<StripeOptions> options,
    ICurrentStripeCustomerIdAccessor stripeCustomerIdAccessor,
    ISubscriptionsService subscriptionsService) : ISubscriptionTransferService
{
    private readonly StripeClient _stripeClient = new(options.Value.ApiKey);

    public async Task<IReadOnlyList<MigratedSubscription>> TransferAsync(
        IReadOnlyList<string> subscriptionIds,
        OrganizationId fromOrganizationId,
        OrganizationId toOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (fromOrganizationId == toOrganizationId || subscriptionIds.Count == 0)
            return [];

        var subscriptionService = new SubscriptionService(_stripeClient);
        var liveOldSubs = new List<Subscription>();
        foreach (var id in subscriptionIds)
        {
            var sub = await TryGetSubscriptionAsync(subscriptionService, id, cancellationToken);
            if (sub is not null && IsMigratable(sub))
                liveOldSubs.Add(sub);
        }

        if (liveOldSubs.Count == 0)
            return subscriptionIds.Select(id => new MigratedSubscription(id, null, null, WasMigrated: false)).ToList();

        var targetCustomerId = await stripeCustomerIdAccessor
            .GetOrCreateStripeCustomerIdForOrganization(toOrganizationId);

        await EnsureTargetHasPaymentMethodAsync(targetCustomerId, cancellationToken);

        // Phase 1 — create every new sub on the target before cancelling ANY old sub.
        // If any create fails, every old sub is still live on the source and we can safely
        // cancel all freshly-created new subs to roll the world back to the pre-transfer state.
        // The rollback must run for EVERY exception, including SubscriptionTransferException
        // (which CreateMigratedSubscriptionAsync throws for malformed source subs) —
        // otherwise an env1 success + env2 throw would leave env1's new sub orphaned on the target.
        var newSubsByOldId = new Dictionary<string, Subscription>();
        try
        {
            foreach (var oldSub in liveOldSubs)
            {
                var newSub = await CreateMigratedSubscriptionAsync(
                    oldSub, targetCustomerId, cancellationToken);
                newSubsByOldId[oldSub.Id] = newSub;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Subscription create failed mid-batch; rolling back {Count} new sub(s)",
                newSubsByOldId.Count);

            foreach (var ns in newSubsByOldId.Values)
                await TryCancelImmediatelyAsync(ns.Id, cancellationToken);

            if (ex is SubscriptionTransferException)
                throw;

            throw new SubscriptionTransferException(
                $"Subscription migration failed; all replacement subscriptions rolled back. Cause: {ex.Message}",
                ex);
        }

        // Phase 2 — cancel old subs. Failures here are best-effort: we DO NOT roll back
        // the new sub for that env because doing so would leave the env with no live
        // subscription (its DB row will be updated to point at the new sub by the caller).
        // A cancel failure means the source customer continues to be billed for that one
        // env until manually cancelled — bounded blast radius, surfaced via the error log.
        foreach (var oldSub in liveOldSubs)
        {
            var newSub = newSubsByOldId[oldSub.Id];
            try
            {
                await CancelOldSubscriptionAsync(
                    oldSub, newSub.Id, toOrganizationId, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed to cancel old subscription {OldSubId} after creating replacement {NewSubId}; " +
                    "source customer will continue to be billed for this environment until manually cancelled.",
                    oldSub.Id, newSub.Id);
            }
        }

        return subscriptionIds
            .Select(id => newSubsByOldId.TryGetValue(id, out var newSub)
                ? new MigratedSubscription(id, newSub.Id, newSub.Status, WasMigrated: true)
                : new MigratedSubscription(id, null, null, WasMigrated: false))
            .ToList();
    }

    private static bool IsMigratable(Subscription sub)
        => sub.Status is "active" or "trialing" or "past_due";

    private static async Task<Subscription?> TryGetSubscriptionAsync(
        SubscriptionService service,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = new SubscriptionGetOptions { Expand = ["items.data.plan", "discounts"] };
            return await service.GetAsync(subscriptionId, options, cancellationToken: cancellationToken);
        }
        catch (StripeException ex) when (ex.Message.Contains("no such subscription", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Subscription {SubscriptionId} not found in Stripe; skipping migration", subscriptionId);
            return null;
        }
    }

    private async Task EnsureTargetHasPaymentMethodAsync(
        string targetCustomerId,
        CancellationToken cancellationToken)
    {
        var service = new PaymentMethodService(_stripeClient);
        var methods = await service.ListAsync(new PaymentMethodListOptions
        {
            Customer = targetCustomerId,
            Type = "card",
            Limit = 1
        }, cancellationToken: cancellationToken);

        if (methods.Data.Count == 0)
            throw new SubscriptionTransferException(
                "The target organization has no payment method on file. Add one before transferring a project with an active subscription.");
    }

    private async Task<Subscription> CreateMigratedSubscriptionAsync(
        Subscription oldSubscription,
        string targetCustomerId,
        CancellationToken cancellationToken)
    {
        var oldItem = oldSubscription.Items?.Data?.FirstOrDefault()
            ?? throw new SubscriptionTransferException(
                $"Subscription {oldSubscription.Id} has no items — cannot determine the price to recreate.");

        var oldPriceId = oldItem.Plan?.Id
            ?? throw new SubscriptionTransferException(
                $"Subscription {oldSubscription.Id} has no plan id — cannot determine the price to recreate.");

        var metadata = new Dictionary<string, string>(oldSubscription.Metadata)
        {
            [StripeMetadataKeys.TransferredFromSubscriptionId] = oldSubscription.Id
        };

        var createOptions = new SubscriptionCreateOptions
        {
            Customer = targetCustomerId,
            Items = [new SubscriptionItemOptions { Price = oldPriceId }],
            Description = oldSubscription.Description,
            Metadata = metadata,
            CollectionMethod = "charge_automatically"
        };

        // Only carry a trial when the old sub was genuinely trialing. For a paid
        // (active/past_due) sub the new one starts paying right away — the source
        // is compensated for the unused period via proration on cancel below.
        if (IsTrialing(oldSubscription))
            createOptions.TrialEnd = oldSubscription.TrialEnd;

        var oldCoupon = oldSubscription.Discounts?.FirstOrDefault()?.Coupon?.Id;
        if (!string.IsNullOrEmpty(oldCoupon))
        {
            createOptions.Discounts =
            [
                new SubscriptionDiscountOptions { Coupon = oldCoupon }
            ];
        }

        var service = new SubscriptionService(_stripeClient);
        return await service.CreateAsync(createOptions, cancellationToken: cancellationToken);
    }

    private static bool IsTrialing(Subscription sub)
        => sub.Status == "trialing"
           || (sub.TrialEnd is DateTime trialEnd && trialEnd > DateTime.UtcNow.AddSeconds(60));

    private async Task CancelOldSubscriptionAsync(
        Subscription oldSubscription,
        string newSubscriptionId,
        OrganizationId targetOrganizationId,
        CancellationToken cancellationToken)
    {
        // Stamp transfer metadata first so the cancellation webhook (and humans
        // inspecting the dashboard) can see why this was cancelled.
        // SubscriptionCancelOptions has no Metadata field, so this must be a separate call.
        var service = new SubscriptionService(_stripeClient);
        await service.UpdateAsync(oldSubscription.Id, new SubscriptionUpdateOptions
        {
            Metadata = new Dictionary<string, string>(oldSubscription.Metadata)
            {
                [StripeMetadataKeys.Transferred] = "true",
                [StripeMetadataKeys.TransferredToSubscriptionId] = newSubscriptionId,
                [StripeMetadataKeys.TransferredToOrganizationId] = targetOrganizationId.Value.ToString()
            }
        }, cancellationToken: cancellationToken);

        // Trialing → no proration (no money changed hands yet anyway).
        // Active/past_due mid-period → prorate=true so the source customer gets credit
        // for the unused remainder of the period they already paid for. The new sub
        // on the target charges immediately for a fresh period.
        var prorate = !IsTrialing(oldSubscription);
        await service.CancelAsync(oldSubscription.Id, new SubscriptionCancelOptions
        {
            Prorate = prorate,
            InvoiceNow = prorate
        }, cancellationToken: cancellationToken);
    }

    private async Task TryCancelImmediatelyAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        try
        {
            await subscriptionsService.CancelSubscriptionAsync(
                subscriptionId,
                cancelImmediately: true,
                cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Compensation cancel failed for subscription {SubscriptionId}; manual cleanup required.",
                subscriptionId);
        }
    }
}
