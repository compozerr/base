using Api.Abstractions;
using Microsoft.Extensions.Options;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Options;
using Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Stripe.Services;

public interface ISubscriptionsService
{
    Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
            CancellationToken cancellationToken = default);

    Task<SubscriptionDto> UpdateSubscriptionTierAsync(
        string subscriptionId,
        ProjectId projectId,
        ServerTierId serverTierId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionDto> CreateSubscriptionTierAsync(
        ProjectId projectId,
        ServerTierId serverTierId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionDto> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately,
        CancellationToken cancellationToken = default);
}

public sealed class SubscriptionsService(
    IOptions<StripeOptions> options,
    IWebHostEnvironment environment,
    ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor) : ISubscriptionsService
{
    private readonly StripeClient _stripeClient = new StripeClient(options.Value.ApiKey);

    private readonly bool _isProduction = environment.IsProduction();

    public async Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
        CancellationToken cancellationToken = default)
    {
        var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

        try
        {
            var service = new Stripe.SubscriptionService(_stripeClient);
            var options = new SubscriptionListOptions
            {
                Customer = stripeCustomerId,
                Expand = ["data.plan.product"]
            };

            var subscriptions = await service.ListAsync(options, cancellationToken: cancellationToken);

            return [.. subscriptions.Select(s => SubscriptionDto.FromSubscription(s, _isProduction))];
        }
        catch (StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
        {
            Log.Warning("No user with id: {StripeCustomerId} found in Stripe", stripeCustomerId);
            return [];
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving subscriptions for user {StripeCustomerId}", stripeCustomerId);
            return [];
        }
    }

    public async Task<SubscriptionDto> UpdateSubscriptionTierAsync(
        string subscriptionId,
        ProjectId projectId,
        ServerTierId serverTierId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new Stripe.SubscriptionService(_stripeClient);

            // Get the subscription item ID
            var subscriptionItemId = await GetSubscriptionItemId(subscriptionId, cancellationToken);

            // Map tier ID to price ID
            var priceId = Prices.GetPriceId(serverTierId.Value, _isProduction);

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscriptionItemId,
                        Price = priceId
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "project_id", projectId.Value.ToString() },
                    { "server_tier_id", serverTierId.Value.ToString() }
                },
                Expand = new List<string> { "plan.product" },
                ProrationBehavior = "create_prorations", // Ensure proration is applied
                ProrationDate = DateTime.UtcNow // Set proration date to now
            };

            var subscription = await service.UpdateAsync(
                subscriptionId,
                options,
                cancellationToken: cancellationToken);

            return SubscriptionDto.FromSubscription(subscription, _isProduction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating subscription {SubscriptionId} to tier {TierId}", subscriptionId, serverTierId.Value);
            throw;
        }
    }

    public async Task<SubscriptionDto> CreateSubscriptionTierAsync(
        ProjectId projectId,
        ServerTierId serverTierId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            var service = new Stripe.SubscriptionService(_stripeClient);
            var options = new SubscriptionCreateOptions
            {
                Customer = stripeCustomerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = Prices.GetPriceId(serverTierId.Value, _isProduction)
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "project_id", projectId.Value.ToString() },
                    { "server_tier_id", serverTierId.Value.ToString() }
                },
                Expand = new List<string> { "items.data.plan.product" }
            };

            var subscription = await service.CreateAsync(options, cancellationToken: cancellationToken);

            if (subscription == null || subscription.Items == null || !subscription.Items.Data.Any())
            {
                throw new Exception("Failed to create subscription or retrieve items.");
            }

            return SubscriptionDto.FromSubscription(subscription, _isProduction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating subscription to tier {TierId}", serverTierId.Value);
            throw;
        }
    }

    public async Task<SubscriptionDto> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new Stripe.SubscriptionService(_stripeClient);

            Subscription subscription;

            if (cancelImmediately)
            {
                // Cancel immediately
                subscription = await service.CancelAsync(subscriptionId, null, null, cancellationToken);
            }
            else
            {
                // Cancel at period end
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true,
                    Expand = new List<string> { "plan.product" }
                };

                subscription = await service.UpdateAsync(subscriptionId, options, cancellationToken: cancellationToken);
            }

            return new SubscriptionDto(
                Id: subscription.Id,
                ProjectId: ProjectId.Create(Guid.Parse(subscription.Metadata["project_id"])),
                Name: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Product?.Name ?? "Subscription",
                Status: subscription.Status,
                PlanId: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "",
                ServerTierId: Prices.GetInternalId(subscription.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "", _isProduction),
                CurrentPeriodStart: subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodStart ?? DateTime.UtcNow,
                CurrentPeriodEnd: subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd ?? DateTime.UtcNow,
                CancelAtPeriodEnd: subscription.CancelAtPeriodEnd,
                Amount: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Amount / 100m ?? 0,
                Currency: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Currency?.ToUpper() ?? "USD"
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error canceling subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    private async Task<string> GetSubscriptionItemId(string subscriptionId, CancellationToken cancellationToken)
    {
        var service = new Stripe.SubscriptionService(_stripeClient);
        var subscription = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);

        return subscription.Items.Data.FirstOrDefault()?.Id
            ?? throw new Exception($"No subscription item found for subscription {subscriptionId}");
    }
}
