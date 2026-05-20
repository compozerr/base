using Api.Abstractions;
using Microsoft.Extensions.Options;
using Organizations.Abstractions;
using Serilog;
using Stripe.Data.Repositories;
using Stripe.Options;

namespace Stripe.Services;

/// <summary>
/// Migrates a project's active Stripe subscription from one organization's Stripe customer
/// to another when the project itself is being transferred between organizations.
///
/// The flow is: create new subscription on target customer with a trial covering the
/// remaining paid period of the old subscription (so no double-billing), then cancel
/// the old subscription with prorate=false (so the source org keeps the money it
/// already paid for the period it already got value from).
///
/// Caller is responsible for: (a) authorising the transfer, (b) flipping
/// Project.OrganizationId. This service only owns the Stripe side. The orchestration
/// rule is: call this BEFORE flipping the project. If this throws, the project flip
/// must not happen.
/// </summary>
public sealed class SubscriptionTransferService(
    IOptions<StripeOptions> options,
    IStripeCustomerRepository stripeCustomerRepository,
    ICurrentStripeCustomerIdAccessor stripeCustomerIdAccessor,
    ISubscriptionsService subscriptionsService) : ISubscriptionTransferService
{
    private readonly StripeClient _stripeClient = new(options.Value.ApiKey);

    public async Task<SubscriptionTransferResult> TransferAsync(
        ProjectId projectId,
        OrganizationId fromOrganizationId,
        OrganizationId toOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (fromOrganizationId == toOrganizationId)
            return new SubscriptionTransferResult(null, WasMigrated: false);

        var sourceCustomerId = await stripeCustomerRepository.GetStripeCustomerIdByInternalIdAsync(
            fromOrganizationId.Value.ToString(),
            cancellationToken);

        // If the source org never had a Stripe customer there is nothing to migrate.
        if (string.IsNullOrEmpty(sourceCustomerId))
            return new SubscriptionTransferResult(null, WasMigrated: false);

        var oldSubscription = await FindActiveSubscriptionForProjectAsync(
            sourceCustomerId, projectId, cancellationToken);

        if (oldSubscription is null)
            return new SubscriptionTransferResult(null, WasMigrated: false);

        var targetCustomerId = await stripeCustomerIdAccessor
            .GetOrCreateStripeCustomerIdForOrganization(toOrganizationId);

        await EnsureTargetHasPaymentMethodAsync(targetCustomerId, cancellationToken);

        var newSubscription = await CreateTrialedSubscriptionAsync(
            oldSubscription, projectId, targetCustomerId, cancellationToken);

        try
        {
            await CancelOldSubscriptionAsync(
                oldSubscription, newSubscription.Id, toOrganizationId, cancellationToken);
        }
        catch (Exception ex)
        {
            // The new sub is live but we couldn't cancel the old one — that would dual-bill.
            // Roll back the new sub and surface the failure.
            Log.Error(ex,
                "Failed to cancel old subscription {OldSubId} after creating {NewSubId}; rolling back new sub.",
                oldSubscription.Id, newSubscription.Id);

            await TryCancelImmediatelyAsync(newSubscription.Id, cancellationToken);
            throw new SubscriptionTransferException(
                $"Could not cancel the original subscription on the source organization. The replacement subscription has been rolled back. Cause: {ex.Message}",
                ex);
        }

        return new SubscriptionTransferResult(
            NewSubscriptionId: newSubscription.Id,
            WasMigrated: true);
    }

    private async Task<Subscription?> FindActiveSubscriptionForProjectAsync(
        string customerId,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        // Auto-page rather than ListAsync — a customer with many cancelled
        // subscriptions can push the active one off the default 10-row first
        // page when Status="all". Truncation would otherwise look like
        // "no sub to migrate" and silently leave the source org billed.
        var service = new SubscriptionService(_stripeClient);
        var options = new SubscriptionListOptions
        {
            Customer = customerId,
            Status = "all",
            Limit = 100,
            Expand = ["data.items.data.plan", "data.discounts"]
        };

        var projectIdString = projectId.Value.ToString();
        await foreach (var subscription in service.ListAutoPagingAsync(options, cancellationToken: cancellationToken))
        {
            if ((subscription.Status == "active" || subscription.Status == "trialing" || subscription.Status == "past_due")
                && subscription.Metadata.TryGetValue(StripeMetadataKeys.ProjectId, out var pid)
                && pid == projectIdString)
            {
                return subscription;
            }
        }

        return null;
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

    private async Task<Subscription> CreateTrialedSubscriptionAsync(
        Subscription oldSubscription,
        ProjectId projectId,
        string targetCustomerId,
        CancellationToken cancellationToken)
    {
        var oldItem = oldSubscription.Items?.Data?.FirstOrDefault()
            ?? throw new SubscriptionTransferException(
                $"Subscription {oldSubscription.Id} has no items — cannot determine the price to recreate.");

        var oldPriceId = oldItem.Plan?.Id
            ?? throw new SubscriptionTransferException(
                $"Subscription {oldSubscription.Id} has no plan id — cannot determine the price to recreate.");

        // Trial through the end of the period the source org has already paid for —
        // they keep that value, the target org isn't charged twice, and the recurring
        // bill resumes naturally on the target customer after the trial ends.
        var trialEnd = oldItem.CurrentPeriodEnd;
        var hasRemainingTrial = trialEnd > DateTime.UtcNow.AddSeconds(60);

        var metadata = new Dictionary<string, string>(oldSubscription.Metadata)
        {
            [StripeMetadataKeys.ProjectId] = projectId.Value.ToString(),
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

        if (hasRemainingTrial)
            createOptions.TrialEnd = trialEnd;

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

    private async Task CancelOldSubscriptionAsync(
        Subscription oldSubscription,
        string newSubscriptionId,
        OrganizationId targetOrganizationId,
        CancellationToken cancellationToken)
    {
        // Stamp transfer metadata first so the cancellation webhook (and humans
        // inspecting the dashboard) can see why this was cancelled.
        // SubscriptionCancelOptions has no Metadata field, so this must be a separate Update call.
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

        // Cancel immediately, no proration refund: the customer already got value
        // for this period, and the new sub trials through that same period so the
        // user isn't double-charged.
        await subscriptionsService.CancelSubscriptionAsync(
            oldSubscription.Id,
            cancelImmediately: true,
            cancellationToken);
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
