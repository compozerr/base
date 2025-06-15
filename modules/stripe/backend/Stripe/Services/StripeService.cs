using Api.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Options;
using StripeSdk = global::Stripe;

namespace Stripe.Services;

public class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeService> _logger;
    private readonly StripeSdk.StripeClient _stripeClient;

    public StripeService(
        IOptions<StripeOptions> options,
        ILogger<StripeService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _stripeClient = new StripeSdk.StripeClient(_options.ApiKey);
    }

    public async Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new StripeSdk.SubscriptionService(_stripeClient);
            var options = new StripeSdk.SubscriptionListOptions
            {
                Customer = userId,
                Expand = new List<string> { "data.plan.product" }
            };

            var subscriptions = await service.ListAsync(options, cancellationToken: cancellationToken);
            return [.. subscriptions.Select(s => new SubscriptionDto(
                                Id: s.Id,
                                Name: s.Items?.Data?.FirstOrDefault()?.Plan?.Product?.Name ?? "Subscription",
                                Status: s.Status,
                                PlanId: s.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "",
                                ServerTierId: GetTierIdFromPriceId(s.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? ""),
                                CurrentPeriodStart: new DateTime(), //s.CurrentPeriodStart,
                                CurrentPeriodEnd: new DateTime(), //s.CurrentPeriodEnd,
                                CancelAtPeriodEnd: s.CancelAtPeriodEnd,
                                Amount: s.Items?.Data?.FirstOrDefault()?.Plan?.Amount / 100m ?? 0,
                                Currency: s.Items?.Data?.FirstOrDefault()?.Plan?.Currency?.ToUpper() ?? "USD"
                ))];
        }
        catch (StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
        {
            _logger.LogWarning("No user with id: {UserId} found in Stripe", userId);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscriptions for user {UserId}", userId);
            return [];
        }
    }

    public async Task<SubscriptionDto> UpdateSubscriptionTierAsync(
        string subscriptionId,
        ServerTierId serverTierId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new StripeSdk.SubscriptionService(_stripeClient);

            // Get the subscription item ID
            var subscriptionItemId = await GetSubscriptionItemId(subscriptionId, cancellationToken);

            // Map tier ID to price ID
            var priceId = GetPriceIdFromTierId(serverTierId.Value);

            var options = new StripeSdk.SubscriptionUpdateOptions
            {
                Items = new List<StripeSdk.SubscriptionItemOptions>
                {
                    new StripeSdk.SubscriptionItemOptions
                    {
                        Id = subscriptionItemId,
                        Price = priceId
                    }
                },
                Expand = new List<string> { "plan.product" }
            };

            var subscription = await service.UpdateAsync(subscriptionId, options, cancellationToken: cancellationToken);

            return new SubscriptionDto(
                Id: subscription.Id,
                Name: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Product?.Name ?? "Subscription",
                Status: subscription.Status,
                PlanId: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "",
                ServerTierId: serverTierId.Value,
                CurrentPeriodStart: new DateTime(), //subscription.CurrentPeriodStart,
                CurrentPeriodEnd: new DateTime(), //subscription.CurrentPeriodEnd,
                CancelAtPeriodEnd: subscription.CancelAtPeriodEnd,
                Amount: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Amount / 100m ?? 0,
                Currency: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Currency?.ToUpper() ?? "USD"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId} to tier {TierId}", subscriptionId, serverTierId.Value);
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
            var service = new StripeSdk.SubscriptionService(_stripeClient);

            StripeSdk.Subscription subscription;

            if (cancelImmediately)
            {
                // Cancel immediately
                subscription = await service.CancelAsync(subscriptionId, null, null, cancellationToken);
            }
            else
            {
                // Cancel at period end
                var options = new StripeSdk.SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true,
                    Expand = new List<string> { "plan.product" }
                };

                subscription = await service.UpdateAsync(subscriptionId, options, cancellationToken: cancellationToken);
            }

            return new SubscriptionDto(
                Id: subscription.Id,
                Name: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Product?.Name ?? "Subscription",
                Status: subscription.Status,
                PlanId: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "",
                ServerTierId: GetTierIdFromPriceId(subscription.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? ""),
                CurrentPeriodStart: new DateTime(), //subscription.CurrentPeriodStart,
                CurrentPeriodEnd: new DateTime(), //subscription.CurrentPeriodEnd,
                CancelAtPeriodEnd: subscription.CancelAtPeriodEnd,
                Amount: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Amount / 100m ?? 0,
                Currency: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Currency?.ToUpper() ?? "USD"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<List<PaymentMethodDto>> GetUserPaymentMethodsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For retrieving payment methods, we don't want to auto-create customers
            // If no customer exists, just return an empty list
            var customerService = new StripeSdk.CustomerService(_stripeClient);
            StripeSdk.Customer customer;
            
            try
            {
                // Try to get the customer
                customer = await customerService.GetAsync(userId, cancellationToken: cancellationToken);
            }
            catch (StripeSdk.StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
            {
                _logger.LogInformation("No customer with id: {UserId} found in Stripe when retrieving payment methods", userId);
                return new List<PaymentMethodDto>();
            }

            // Get the default payment method ID
            string? defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;

            // Retrieve all payment methods for the customer
            var service = new StripeSdk.PaymentMethodService(_stripeClient);
            var options = new StripeSdk.PaymentMethodListOptions
            {
                Customer = userId,
                Type = "card"
            };

            var paymentMethods = await service.ListAsync(options, cancellationToken: cancellationToken);

            return paymentMethods.Select(pm => new PaymentMethodDto
            {
                Id = pm.Id,
                Type = pm.Type,
                Brand = pm.Card?.Brand ?? "",
                Last4 = pm.Card?.Last4 ?? "",
                ExpiryMonth = (int?)pm.Card?.ExpMonth ?? 0,
                ExpiryYear = (int?)pm.Card?.ExpYear ?? 0,
                IsDefault = pm.Id == defaultPaymentMethodId
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods for user {UserId}", userId);
            return new List<PaymentMethodDto>();
        }
    }

    public async Task<PaymentMethodDto> AddPaymentMethodAsync(
        string userId,
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the customer exists in Stripe (create if needed)
            string stripeCustomerId = await EnsureCustomerExistsAsync(userId, cancellationToken);
            
            var service = new StripeSdk.PaymentMethodService(_stripeClient);
            
            // Attach the payment method to the customer
            var options = new StripeSdk.PaymentMethodAttachOptions
            {
                Customer = stripeCustomerId
            };

            var paymentMethod = await service.AttachAsync(paymentMethodId, options, cancellationToken: cancellationToken);

            // If this is the first payment method, make it the default
            var paymentMethods = await GetUserPaymentMethodsAsync(stripeCustomerId, cancellationToken);
            if (paymentMethods.Count == 1)
            {
                await SetDefaultPaymentMethodAsync(stripeCustomerId, paymentMethodId, cancellationToken);
                return await GetPaymentMethod(paymentMethodId, stripeCustomerId, cancellationToken);
            }

            return new PaymentMethodDto
            {
                Id = paymentMethod.Id,
                Type = paymentMethod.Type,
                Brand = paymentMethod.Card?.Brand ?? "",
                Last4 = paymentMethod.Card?.Last4 ?? "",
                ExpiryMonth = (int?)paymentMethod.Card?.ExpMonth ?? 0,
                ExpiryYear = (int?)paymentMethod.Card?.ExpYear ?? 0,
                IsDefault = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment method {PaymentMethodId} for user {UserId}", paymentMethodId, userId);
            throw;
        }
    }

    public async Task<bool> RemovePaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new StripeSdk.PaymentMethodService(_stripeClient);

            // First retrieve the payment method to get customer ID
            var paymentMethod = await service.GetAsync(paymentMethodId, cancellationToken: cancellationToken);

            // Detach the payment method
            await service.DetachAsync(paymentMethodId, null, null, cancellationToken);

            // If this was the default payment method, set another one as default if available
            if (paymentMethod.CustomerId != null)
            {
                var methods = await GetUserPaymentMethodsAsync(paymentMethod.CustomerId, cancellationToken);
                if (methods.Count > 0)
                {
                    await SetDefaultPaymentMethodAsync(paymentMethod.CustomerId, methods[0].Id, cancellationToken);
                }
            }

            return true;
        }
        catch (StripeSdk.StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
        {
            _logger.LogWarning("Customer associated with payment method {PaymentMethodId} not found in Stripe", paymentMethodId);
            // For removal, just return true as if successfully removed since it doesn't exist anyway
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment method {PaymentMethodId}", paymentMethodId);
            throw;
        }
    }

    public async Task<PaymentMethodDto> SetDefaultPaymentMethodAsync(
        string userId,
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the customer exists in Stripe (create if needed)
            string stripeCustomerId = await EnsureCustomerExistsAsync(userId, cancellationToken);
            
            var customerService = new StripeSdk.CustomerService(_stripeClient);

            // Update the customer's default payment method
            var options = new StripeSdk.CustomerUpdateOptions
            {
                InvoiceSettings = new StripeSdk.CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            };

            await customerService.UpdateAsync(stripeCustomerId, options, cancellationToken: cancellationToken);

            // Return the updated payment method
            return await GetPaymentMethod(paymentMethodId, stripeCustomerId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method {PaymentMethodId} for user {UserId}", paymentMethodId, userId);
            throw;
        }
    }

    #region Private Helper Methods

    private string GetPriceIdFromTierId(string tierId)
    {
        // Map tier IDs to price IDs
        var priceTierMap = new Dictionary<string, string>
        {
            {"basic", "price_basic_monthly"},
            {"standard", "price_standard_monthly"},
            {"professional", "price_professional_monthly"},
            {"enterprise", "price_enterprise_monthly"}
        };

        if (priceTierMap.TryGetValue(tierId.ToLower(), out var priceId))
        {
            return priceId;
        }

        throw new ArgumentException($"No price found for tier ID {tierId}");
    }

    private string GetTierIdFromPriceId(string priceId)
    {
        // Map Stripe price IDs back to tier IDs
        var tierPriceMap = new Dictionary<string, string>
        {
            {"price_basic_monthly", "basic"},
            {"price_standard_monthly", "standard"},
            {"price_professional_monthly", "professional"},
            {"price_enterprise_monthly", "enterprise"}
        };

        if (tierPriceMap.TryGetValue(priceId.ToLower(), out var tierId))
        {
            return tierId;
        }

        return "basic"; // Default fallback
    }

    private async Task<string> GetSubscriptionItemId(string subscriptionId, CancellationToken cancellationToken)
    {
        var service = new StripeSdk.SubscriptionService(_stripeClient);
        var subscription = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);

        return subscription.Items.Data.FirstOrDefault()?.Id
            ?? throw new Exception($"No subscription item found for subscription {subscriptionId}");
    }

    private async Task<PaymentMethodDto> GetPaymentMethod(string paymentMethodId, string userId, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure the customer exists in Stripe (create if needed)
            string stripeCustomerId = await EnsureCustomerExistsAsync(userId, cancellationToken);
            
            var service = new StripeSdk.PaymentMethodService(_stripeClient);
            var customerService = new StripeSdk.CustomerService(_stripeClient);

            // Get the payment method
            var paymentMethod = await service.GetAsync(paymentMethodId, cancellationToken: cancellationToken);

            // Get the customer to check if this is the default payment method
            var customer = await customerService.GetAsync(stripeCustomerId, cancellationToken: cancellationToken);
            string? defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;

            return new PaymentMethodDto
            {
                Id = paymentMethod.Id,
                Type = paymentMethod.Type,
                Brand = paymentMethod.Card?.Brand ?? "",
                Last4 = paymentMethod.Card?.Last4 ?? "",
                ExpiryMonth = (int?)paymentMethod.Card?.ExpMonth ?? 0,
                ExpiryYear = (int?)paymentMethod.Card?.ExpYear ?? 0,
                IsDefault = paymentMethod.Id == defaultPaymentMethodId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method {PaymentMethodId} for user {UserId}", paymentMethodId, userId);
            throw;
        }
    }

    /// <summary>
    /// Ensures that a customer exists in Stripe with the given ID
    /// </summary>
    /// <param name="stripeCustomerId">The user ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The Stripe customer ID (may be different from userId if created)</returns>
    private async Task<string> EnsureCustomerExistsAsync(string stripeCustomerId, CancellationToken cancellationToken = default)
    {
        var customerService = new StripeSdk.CustomerService(_stripeClient);

        try
        {
            // Try to retrieve the customer
            await customerService.GetAsync(stripeCustomerId, cancellationToken: cancellationToken);
            return stripeCustomerId; // Customer exists, return the same ID
        }
        catch (StripeSdk.StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
        {
            // Customer doesn't exist, create a new one
            _logger.LogInformation("Customer {UserId} not found in Stripe, creating new customer", stripeCustomerId);
            
            var customerOptions = new StripeSdk.CustomerCreateOptions
            {
                Description = $"Auto-created for user {stripeCustomerId}"
            };
            
            var customer = await customerService.CreateAsync(customerOptions, cancellationToken: cancellationToken);
            _logger.LogInformation("Created Stripe customer with ID {StripeCustomerId} for user {UserId}", customer.Id, stripeCustomerId);
            
            return customer.Id;
        }
    }

    #endregion
}
