using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Options;

namespace Stripe.Services;

public interface IStripeCustomerValidationService
{
    /// <summary>
    /// Checks if a Stripe customer has a default payment method set up
    /// </summary>
    /// <param name="customerId">The Stripe customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if customer has a default payment method, false otherwise</returns>
    Task<bool> HasDefaultPaymentMethodAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a Stripe invoice has been paid
    /// </summary>
    /// <param name="invoiceId">The Stripe invoice ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if invoice has been paid, false otherwise</returns>
    Task<bool> IsInvoicePaidAsync(string invoiceId, CancellationToken cancellationToken = default);
}

public sealed class StripeCustomerValidationService(IOptions<StripeOptions> options) : IStripeCustomerValidationService
{
    private readonly StripeClient _stripeClient = new(options.Value.ApiKey);

    public async Task<bool> HasDefaultPaymentMethodAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customerService = new CustomerService(_stripeClient);
            var customer = await customerService.GetAsync(customerId, cancellationToken: cancellationToken);

            // Check if customer has a default payment method
            if (!string.IsNullOrEmpty(customer.InvoiceSettings?.DefaultPaymentMethodId))
            {
                return true;
            }

            // Also check if customer has any payment methods attached
            var paymentMethodService = new PaymentMethodService(_stripeClient);
            var paymentMethodOptions = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card",
                Limit = 1
            };

            var paymentMethods = await paymentMethodService.ListAsync(paymentMethodOptions, cancellationToken: cancellationToken);
            return paymentMethods.Data.Count > 0;
        }
        catch (StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such customer"))
        {
            Log.Warning("Customer {CustomerId} not found in Stripe when checking for payment method", customerId);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if customer {CustomerId} has default payment method", customerId);
            return false;
        }
    }

    public async Task<bool> IsInvoicePaidAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoiceService = new InvoiceService(_stripeClient);
            var invoice = await invoiceService.GetAsync(invoiceId, cancellationToken: cancellationToken);

            // Check if invoice status is 'paid'
            return invoice.Status == "paid";
        }
        catch (StripeException ex) when (ex.Message.ToLowerInvariant().Contains("no such invoice"))
        {
            Log.Warning("Invoice {InvoiceId} not found in Stripe when checking payment status", invoiceId);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if invoice {InvoiceId} is paid", invoiceId);
            return false;
        }
    }
}
