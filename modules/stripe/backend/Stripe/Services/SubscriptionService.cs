using Api.Abstractions;
using Microsoft.Extensions.Options;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Options;
using Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Auth.Abstractions;
using Stripe.Events;

namespace Stripe.Services;

public interface ISubscriptionsService
{
    Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
            CancellationToken cancellationToken = default);
    Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionDto> UpdateSubscriptionTierAsync(
        string subscriptionId,
        ProjectId projectId,
        ServerTierId serverTierId,
        string? couponCode = null,
        CancellationToken cancellationToken = default);

    Task<SubscriptionDto> CreateSubscriptionTierAsync(
        ProjectId projectId,
        ServerTierId serverTierId,
        string? couponCode = null,
        CancellationToken cancellationToken = default);

    Task<SubscriptionDto> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately,
        CancellationToken cancellationToken = default);
}

public sealed class SubscriptionsService(
    IOptions<StripeOptions> options,
    IWebHostEnvironment environment,
    IPaymentMethodsService paymentMethodsService,
    ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor,
    IPaymentFailureSagaOrchestrator sagaOrchestrator) : ISubscriptionsService
{
    private readonly StripeClient _stripeClient = new StripeClient(options.Value.ApiKey);

    private readonly bool _isProduction = environment.IsProduction();

    public async Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
        CancellationToken cancellationToken = default)
    {
        var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();
        return await GetSubscriptionsForStripeCustomerId(stripeCustomerId, cancellationToken);
    }

    public async Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId(userId);
        return await GetSubscriptionsForStripeCustomerId(stripeCustomerId, cancellationToken);
    }

    private async Task<List<SubscriptionDto>> GetSubscriptionsForStripeCustomerId(string stripeCustomerId, CancellationToken cancellationToken)
    {
        try
        {
            var service = new Stripe.SubscriptionService(_stripeClient);
            var options = new SubscriptionListOptions
            {
                Customer = stripeCustomerId,
                Expand = ["data.plan.product", "data.discounts"]
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
        string? couponCode = null,
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
                Discounts = !string.IsNullOrWhiteSpace(couponCode) ?
                [
                    new SubscriptionDiscountOptions
                    {
                        Coupon = couponCode
                    }
                ] : null,
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
        string? couponCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            // Check if customer has a payment method
            bool hasPaymentMethod = await GetUserHasPaymentMethodAsync(cancellationToken);

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
                    { "server_tier_id", serverTierId.Value.ToString() },
                    { "awaiting_payment_method", hasPaymentMethod ? "false" : "true" }
                },
                Expand = new List<string> { "items.data.plan.product" }
            };

            // Set collection method based on payment method availability
            if (hasPaymentMethod)
            {
                options.CollectionMethod = "charge_automatically";
            }
            else
            {
                options.TrialPeriodDays = 5; // Give user 5 days to add a payment method
                options.CollectionMethod = "charge_automatically";
                options.PaymentBehavior = "allow_incomplete";
            }

            // Apply coupon code if provided
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                options.Discounts = new List<SubscriptionDiscountOptions>
                {
                    new SubscriptionDiscountOptions
                    {
                        Coupon = couponCode
                    }
                };
            }

            var subscription = await service.CreateAsync(options, cancellationToken: cancellationToken);

            if (subscription == null || subscription.Items == null || !subscription.Items.Data.Any())
            {
                throw new Exception("Failed to create subscription or retrieve items.");
            }

            // Start payment failure saga if no payment method is set up
            if (!hasPaymentMethod)
            {
                await StartSagaForSubscriptionWithoutPaymentMethod(
                    subscription,
                    stripeCustomerId,
                    cancellationToken);
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
                    Expand = new List<string> { "plan.product", "discounts" }
                };

                subscription = await service.UpdateAsync(subscriptionId, options, cancellationToken: cancellationToken);
            }

            return SubscriptionDto.FromSubscription(subscription, _isProduction);
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

    private async Task<bool> GetUserHasPaymentMethodAsync(CancellationToken cancellationToken)
    {
        var paymentMethods = await paymentMethodsService.GetUserPaymentMethodsAsync(
            cancellationToken);

        return paymentMethods.Any();
    }

    private async Task StartSagaForSubscriptionWithoutPaymentMethod(
        Subscription subscription,
        string stripeCustomerId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to get the latest invoice for this subscription
            var invoiceService = new InvoiceService(_stripeClient);
            var invoiceOptions = new InvoiceListOptions
            {
                Subscription = subscription.Id,
                Limit = 1
            };

            var invoices = await invoiceService.ListAsync(invoiceOptions, cancellationToken: cancellationToken);
            var latestInvoice = invoices.Data.FirstOrDefault();

            string invoiceId;
            decimal amountDue;
            string currency;
            string paymentLink;

            if (latestInvoice != null)
            {
                // Use actual invoice details if available
                invoiceId = latestInvoice.Id;
                amountDue = latestInvoice.AmountDue / 100m;
                currency = latestInvoice.Currency;
                paymentLink = latestInvoice.HostedInvoiceUrl ?? string.Empty;
            }
            else
            {
                // No invoice yet (trial period) - use placeholder values
                Log.Information(
                    "No invoice found for subscription {SubscriptionId} (trial period), starting saga with placeholder values",
                    subscription.Id);

                invoiceId = $"pending_{subscription.Id}";

                // Get the subscription amount from the price
                var firstItem = subscription.Items.Data.FirstOrDefault();
                if (firstItem?.Price?.UnitAmount != null)
                {
                    amountDue = firstItem.Price.UnitAmount.Value / 100m;
                    currency = firstItem.Price.Currency ?? "usd";
                }
                else
                {
                    amountDue = 0m;
                    currency = "usd";
                }

                paymentLink = string.Empty;
            }

            Log.Information(
                "Starting payment failure saga for subscription {SubscriptionId} without payment method",
                subscription.Id);

            await sagaOrchestrator.StartSagaForNewSubscriptionAsync(
                subscription.Id,
                stripeCustomerId,
                invoiceId,
                amountDue,
                currency,
                paymentLink,
                cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error starting payment failure saga for subscription {SubscriptionId}",
                subscription.Id);
        }
    }
}