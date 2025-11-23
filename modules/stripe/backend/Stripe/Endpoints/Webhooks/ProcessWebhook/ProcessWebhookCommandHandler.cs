using Core.MediatR;
using MediatR;
using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Events;
using Stripe.Jobs;
using Stripe.Options;

namespace Stripe.Endpoints.Webhooks.ProcessWebhook;

public class ProcessWebhookCommandHandler(
    IOptions<StripeOptions> stripeOptions,
    IPublisher publisher) : ICommandHandler<ProcessWebhookCommand>
{
    private readonly ILogger _logger = Log.ForContext<ProcessWebhookCommandHandler>();
    private readonly StripeClient _stripeClient = new StripeClient(stripeOptions.Value.ApiKey);

    public async Task Handle(
        ProcessWebhookCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                request.PayloadJson,
                request.StripeSignature,
                stripeOptions.Value.WebhookEndpointSecret);

            _logger.Information("Processing Stripe webhook event: {EventType} with ID: {EventId}",
                stripeEvent.Type, stripeEvent.Id);

            await ProcessStripeEvent(stripeEvent, cancellationToken);
        }
        catch (StripeException ex)
        {
            _logger.Error(ex, "Failed to process Stripe webhook: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error processing Stripe webhook");
            throw;
        }
    }

    private async Task ProcessStripeEvent(Event stripeEvent, CancellationToken cancellationToken)
    {
        switch (stripeEvent.Type)
        {
            case "invoice.payment_failed":
                HandleInvoicePaymentFailed(stripeEvent);
                break;

            case "invoice.payment_succeeded":
                _logger.Information("Invoice payment succeeded for invoice: {InvoiceId}",
                    ((Invoice)stripeEvent.Data.Object).Id);
                break;

            case "customer.updated":
                await HandleCustomerUpdated(
                    stripeEvent,
                    cancellationToken);
                break;

            default:
                _logger.Information("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private void HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = (Invoice)stripeEvent.Data.Object;

        _logger.Warning("Invoice payment failed - Invoice: {InvoiceId}, Customer: {CustomerId}, Amount: {Amount}",
            invoice.Id, invoice.CustomerId, invoice.AmountDue);

        string subscriptionId = "";
        if (invoice.Lines?.Data?.Count > 0)
        {
            var line = invoice.Lines.Data.FirstOrDefault();
            subscriptionId = line?.SubscriptionId ?? "";
        }

        var dueDate = invoice.DueDate ?? DateTime.UtcNow;

        var daysOverdue = (DateTime.UtcNow - dueDate).Days;

        var failedPaymentEvent = new StripeInvoicePaymentFailedEvent(
            InvoiceId: invoice.Id,
            CustomerId: invoice.CustomerId,
            SubscriptionId: subscriptionId,
            AmountDue: invoice.AmountDue / 100m,
            Currency: invoice.Currency,
            DueDate: dueDate,
            DaysOverdue: daysOverdue,
            PaymentLink: invoice.HostedInvoiceUrl ?? "",
            AttemptCount: (int)invoice.AttemptCount,
            FailureReason: invoice.LastFinalizationError?.Message,
            NextPaymentAttempt: invoice.NextPaymentAttempt);

        StripeInvoicePaymentFailedJob.Enqueue(
            failedPaymentEvent);

        _logger.Information("Published StripeInvoicePaymentFailedEvent for invoice: {InvoiceId}", invoice.Id);
    }

    private async Task HandleCustomerUpdated(Event stripeEvent, CancellationToken cancellationToken)
    {
        var customer = (Customer)stripeEvent.Data.Object;

        _logger.Information("Customer updated - Customer: {CustomerId}, Has Payment Method: {HasPaymentMethod}",
            customer.Id, customer.InvoiceSettings?.DefaultPaymentMethodId != null);

        // Only process if a payment method was added
        if (string.IsNullOrEmpty(customer.InvoiceSettings?.DefaultPaymentMethodId))
        {
            _logger.Information("No default payment method set for customer: {CustomerId}, skipping invoice payment", customer.Id);
            return;
        }

        // Get all open invoices for this customer
        var invoiceService = new InvoiceService(_stripeClient);
        var invoiceListOptions = new InvoiceListOptions
        {
            Customer = customer.Id,
            Status = "open",
            Limit = 100 // Adjust if needed
        };

        var openInvoices = await invoiceService.ListAsync(invoiceListOptions, cancellationToken: cancellationToken);

        if (openInvoices.Data.Count == 0)
        {
            _logger.Information("No open invoices found for customer: {CustomerId}", customer.Id);
            return;
        }

        _logger.Information("Found {Count} open invoices for customer: {CustomerId}, attempting to pay them",
            openInvoices.Data.Count, customer.Id);

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
            catch (StripeException ex)
            {
                _logger.Error(ex, "Failed to pay invoice: {InvoiceId} for customer: {CustomerId}, Reason: {Reason}",
                    invoice.Id, customer.Id, ex.Message);
                // Continue trying to pay other invoices even if one fails
            }
        }
    }
}
