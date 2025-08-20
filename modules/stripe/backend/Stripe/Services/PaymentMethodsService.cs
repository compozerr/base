using Microsoft.Extensions.Options;
using Stripe.Options;
using Serilog;

namespace Stripe.Services;

public interface IPaymentMethodsService
{
    Task<List<PaymentMethodDto>> GetUserPaymentMethodsAsync(
        CancellationToken cancellationToken = default);

    Task<PaymentMethodDto> AddPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    Task<bool> RemovePaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    Task<PaymentMethodDto> SetDefaultPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    Task<string> CreateSetupIntentAsync(
        CancellationToken cancellationToken = default);

}

public class PaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // card, bank_account, etc.
    public string Brand { get; set; } = string.Empty; // Visa, Mastercard, etc.
    public string Last4 { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class PaymentMethodsService(
    IOptions<StripeOptions> options,
    ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor) : IPaymentMethodsService
{
    private readonly StripeClient _stripeClient = new(options.Value.ApiKey);

    public async Task<List<PaymentMethodDto>> GetUserPaymentMethodsAsync(
          CancellationToken cancellationToken = default)
    {
        try
        {
            // For retrieving payment methods, we don't want to auto-create customers
            // If no customer exists, just return an empty list
            var customerService = new CustomerService(_stripeClient);
            Customer customer;

            var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

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
            string stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

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
            string stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

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

    public async Task<string> CreateSetupIntentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

            var service = new SetupIntentService(_stripeClient);
            var options = new SetupIntentCreateOptions
            {
                Customer = stripeCustomerId,
                PaymentMethodTypes = new List<string> { "card" },
                Usage = "off_session",
                Confirm = false,
                AutomaticPaymentMethods = new SetupIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = false
                }
            };

            var setupIntent = await service.CreateAsync(options, cancellationToken: cancellationToken);
            return setupIntent.ClientSecret;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating setup intent for user");
            throw;
        }
    }

    private async Task<PaymentMethodDto> GetPaymentMethod(string paymentMethodId, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure the customer exists in Stripe (create if needed)
            string stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId();

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
}
