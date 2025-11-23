using Core.Abstractions;
using Microsoft.Extensions.Options;
using Stripe.Events;
using Stripe.Options;

namespace Api.EventHandlers.Stripe;

public class StripeCustomerPaymentMethodAddedEventHandler(
    IOptions<StripeOptions> stripeOptions) : IEventHandler<StripeCustomerPaymentMethodAddedEvent>
{
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<StripeCustomerPaymentMethodAddedEventHandler>();
    private readonly global::Stripe.StripeClient _stripeClient = new global::Stripe.StripeClient(stripeOptions.Value.ApiKey);

    public async Task Handle(
        StripeCustomerPaymentMethodAddedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.Information("Processing payment method added - Customer: {CustomerId}, Payment Method: {PaymentMethodId}",
            notification.CustomerId,
            notification.PaymentMethodId);

        try
        {
            // Get all open invoices for this customer
            var invoiceService = new global::Stripe.InvoiceService(_stripeClient);
            var invoiceListOptions = new global::Stripe.InvoiceListOptions
            {
                Customer = notification.CustomerId,
                Status = "open",
                Limit = 100 // Adjust if needed
            };

            var openInvoices = await invoiceService.ListAsync(invoiceListOptions, cancellationToken: cancellationToken);

            if (openInvoices.Data.Count == 0)
            {
                _logger.Information("No open invoices found for customer: {CustomerId}", notification.CustomerId);
                return;
            }

            _logger.Information("Found {Count} open invoices for customer: {CustomerId}, attempting to pay them",
                openInvoices.Data.Count, notification.CustomerId);

            // Attempt to pay each open invoice
            foreach (var invoice in openInvoices.Data)
            {
                try
                {
                    _logger.Information("Attempting to pay invoice: {InvoiceId} for amount: {Amount} {Currency}",
                        invoice.Id, invoice.AmountDue / 100m, invoice.Currency);

                    var paidInvoice = await invoiceService.PayAsync(invoice.Id, cancellationToken: cancellationToken);

                    _logger.Information("Successfully paid invoice: {InvoiceId}, Status: {Status}",
                        paidInvoice.Id, paidInvoice.Status);
                }
                catch (global::Stripe.StripeException ex)
                {
                    _logger.Error(ex, "Failed to pay invoice: {InvoiceId} for customer: {CustomerId}, Reason: {Reason}",
                        invoice.Id, notification.CustomerId, ex.Message);
                    // Continue trying to pay other invoices even if one fails
                }
            }

            _logger.Information("Completed processing payment method added for customer: {CustomerId}", notification.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling payment method added for customer: {CustomerId}", notification.CustomerId);
            throw;
        }
    }
}
