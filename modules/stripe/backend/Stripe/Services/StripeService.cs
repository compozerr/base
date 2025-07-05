using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Events;
using Stripe.Options;

namespace Stripe.Services;

public class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly StripeClient _stripeClient;
    private readonly ICurrentStripeCustomerIdAccessor _currentStripeCustomerIdAccessor;

    private readonly bool _isProduction;

    public StripeService(
        IOptions<StripeOptions> options,
        IWebHostEnvironment environment,
        ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor)
    {
        _options = options.Value;
        _stripeClient = new StripeClient(_options.ApiKey);
        _currentStripeCustomerIdAccessor = currentStripeCustomerIdAccessor;
        _isProduction = environment.IsProduction();
    }

    public async Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
        CancellationToken cancellationToken = default)
    {
        var stripeCustomerId = await _currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

        try
        {
            var service = new SubscriptionService(_stripeClient);
            var options = new SubscriptionListOptions
            {
                Customer = stripeCustomerId,
                Expand = new List<string> { "data.plan.product" }
            };


            var subscriptions = await service.ListAsync(options, cancellationToken: cancellationToken);

            return [.. subscriptions.Select(s => new SubscriptionDto(
                                Id: s.Id,
                                ProjectId: ProjectId.Create(Guid.Parse(s.Metadata["project_id"])),
                                Name: s.Items?.Data?.FirstOrDefault()?.Plan?.Product?.Name ?? "Subscription",
                                Status: s.Status,
                                PlanId: s.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "",
                                ServerTierId: Prices.GetInternalId(s.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "", _isProduction),
                                CurrentPeriodStart: s.Items?.Data?.FirstOrDefault()?.CurrentPeriodStart ?? DateTime.UtcNow,
                                CurrentPeriodEnd: s.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd ?? DateTime.UtcNow,
                                CancelAtPeriodEnd: s.CancelAtPeriodEnd,
                                Amount: s.Items?.Data?.FirstOrDefault()?.Plan?.Amount / 100m ?? 0,
                                Currency: s.Items?.Data?.FirstOrDefault()?.Plan?.Currency?.ToUpper() ?? "USD"
                ))];
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
            var service = new SubscriptionService(_stripeClient);

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

            return new SubscriptionDto(
                Id: subscription.Id,
                ProjectId: projectId,
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
            string stripeCustomerId = await _currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            var service = new SubscriptionService(_stripeClient);
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

            return new SubscriptionDto(
                Id: subscription.Id,
                ProjectId: projectId,
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
            var service = new SubscriptionService(_stripeClient);

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
                CurrentPeriodStart: new DateTime(), //subscription.CurrentPeriodStart,
                CurrentPeriodEnd: new DateTime(), //subscription.CurrentPeriodEnd,
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

    public async Task<List<PaymentMethodDto>> GetUserPaymentMethodsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For retrieving payment methods, we don't want to auto-create customers
            // If no customer exists, just return an empty list
            var customerService = new CustomerService(_stripeClient);
            Customer customer;

            var stripeCustomerId = await _currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            try
            {
                customer = await customerService.GetAsync(stripeCustomerId, cancellationToken: cancellationToken);
            }
            catch (StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
            {
                Log.Information("No customer with id: {StripeCustomerId} found in Stripe when retrieving payment methods", stripeCustomerId);
                return new List<PaymentMethodDto>();
            }

            // Get the default payment method ID
            string? defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;

            // Retrieve all payment methods for the customer
            var service = new PaymentMethodService(_stripeClient);
            var options = new PaymentMethodListOptions
            {
                Customer = stripeCustomerId,
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
            Log.Error(ex, "Error retrieving payment methods for user");
            return new List<PaymentMethodDto>();
        }
    }

    public async Task<PaymentMethodDto> AddPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the customer exists in Stripe (create if needed)
            string stripeCustomerId = await _currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            var service = new PaymentMethodService(_stripeClient);

            // Attach the payment method to the customer
            var options = new PaymentMethodAttachOptions
            {
                Customer = stripeCustomerId
            };

            var paymentMethod = await service.AttachAsync(paymentMethodId, options, cancellationToken: cancellationToken);

            // If this is the first payment method, make it the default
            var paymentMethods = await GetUserPaymentMethodsAsync(cancellationToken);
            if (paymentMethods.Count == 1)
            {
                await SetDefaultPaymentMethodAsync(paymentMethodId, cancellationToken);
                return await GetPaymentMethod(paymentMethodId, cancellationToken);
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
            Log.Error(ex, "Error adding payment method {PaymentMethodId} for user", paymentMethodId);
            throw;
        }
    }

    public async Task<bool> RemovePaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentMethodService(_stripeClient);

            // First retrieve the payment method to get customer ID
            var paymentMethod = await service.GetAsync(paymentMethodId, cancellationToken: cancellationToken);

            // Detach the payment method
            await service.DetachAsync(paymentMethodId, null, null, cancellationToken);

            // If this was the default payment method, set another one as default if available
            if (paymentMethod.CustomerId != null)
            {
                var methods = await GetUserPaymentMethodsAsync(cancellationToken);
                if (methods.Count > 0)
                {
                    await SetDefaultPaymentMethodAsync(methods[0].Id, cancellationToken);
                }
            }

            return true;
        }
        catch (StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
        {
            Log.Warning("Customer associated with payment method {PaymentMethodId} not found in Stripe", paymentMethodId);
            // For removal, just return true as if successfully removed since it doesn't exist anyway
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error removing payment method {PaymentMethodId}", paymentMethodId);
            throw;
        }
    }

    public async Task<PaymentMethodDto> SetDefaultPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the customer exists in Stripe (create if needed)
            string stripeCustomerId = await _currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            var customerService = new CustomerService(_stripeClient);

            // Update the customer's default payment method
            var options = new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            };

            await customerService.UpdateAsync(stripeCustomerId, options, cancellationToken: cancellationToken);

            // Return the updated payment method
            return await GetPaymentMethod(paymentMethodId, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting default payment method {PaymentMethodId} for user", paymentMethodId);
            throw;
        }
    }

    #region Private Helper Methods



    private async Task<string> GetSubscriptionItemId(string subscriptionId, CancellationToken cancellationToken)
    {
        var service = new SubscriptionService(_stripeClient);
        var subscription = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);

        return subscription.Items.Data.FirstOrDefault()?.Id
            ?? throw new Exception($"No subscription item found for subscription {subscriptionId}");
    }

    private async Task<PaymentMethodDto> GetPaymentMethod(string paymentMethodId, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure the customer exists in Stripe (create if needed)
            string stripeCustomerId = await _currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            var service = new PaymentMethodService(_stripeClient);
            var customerService = new CustomerService(_stripeClient);

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
            Log.Error(ex, "Error retrieving payment method {PaymentMethodId} for user", paymentMethodId);
            throw;
        }
    }
    #endregion
}
