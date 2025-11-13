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
                await HandleInvoicePaymentFailed(
                    stripeEvent,
                    cancellationToken);
                break;

            case "invoice.payment_succeeded":
                _logger.Information("Invoice payment succeeded for invoice: {InvoiceId}",
                    ((Invoice)stripeEvent.Data.Object).Id);
                break;

            default:
                _logger.Information("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent, CancellationToken cancellationToken)
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
}
