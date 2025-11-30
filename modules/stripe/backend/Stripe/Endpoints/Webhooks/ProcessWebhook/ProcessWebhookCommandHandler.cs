using Core.MediatR;
using MediatR;
using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Events;
using Stripe.Jobs;
using Stripe.Options;

namespace Stripe.Endpoints.Webhooks.ProcessWebhook;

public class ProcessWebhookCommandHandler(
    IOptions<StripeOptions> stripeOptions) : ICommandHandler<ProcessWebhookCommand>
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
                HandleInvoicePaymentSucceeded(stripeEvent);
                break;

            case "customer.updated":
                HandleCustomerUpdated(stripeEvent);
                break;

            case "customer.subscription.deleted":
                HandleSubscriptionDeleted(stripeEvent);
                break;

            default:
                _logger.Information("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private void HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = (Invoice)stripeEvent.Data.Object;
        if(invoice.AmountPaid == 0)
        {
            _logger.Information("Invoice payment succeeded but amount paid is zero - Invoice: {InvoiceId}, Customer: {CustomerId}", invoice.Id, invoice.CustomerId);
            return;
        }

        _logger.Information("Invoice payment succeeded - Invoice: {InvoiceId}, Customer: {CustomerId}, Amount: {Amount}",
            invoice.Id, invoice.CustomerId, invoice.AmountPaid);

        string subscriptionId = "";
        if (invoice.Lines?.Data?.Count > 0)
        {
            var line = invoice.Lines.Data.FirstOrDefault();
            subscriptionId = line?.SubscriptionId ?? "";
        }

        var paidAt = invoice.StatusTransitions.PaidAt ?? DateTime.UtcNow;

        var paymentSucceededEvent = new StripeInvoicePaymentSucceededEvent(
            InvoiceId: invoice.Id,
            CustomerId: invoice.CustomerId,
            SubscriptionId: subscriptionId,
            AmountPaid: invoice.AmountPaid / 100m,
            Currency: invoice.Currency,
            PaidAt: paidAt,
            InvoiceLink: invoice.HostedInvoiceUrl
        );

        StripeInvoicePaymentSucceededJob.Enqueue(
            paymentSucceededEvent);

        _logger.Information("Published StripeInvoicePaymentSucceededEvent for invoice: {InvoiceId}", invoice.Id);
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

    private void HandleCustomerUpdated(Event stripeEvent)
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

        var paymentMethodAddedEvent = new StripeCustomerPaymentMethodAddedEvent(
            CustomerId: customer.Id,
            PaymentMethodId: customer.InvoiceSettings.DefaultPaymentMethodId);

        StripeCustomerPaymentMethodAddedJob.Enqueue(paymentMethodAddedEvent);

        _logger.Information("Enqueued StripeCustomerPaymentMethodAddedEvent for customer: {CustomerId}", customer.Id);
    }

    private void HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = (Subscription)stripeEvent.Data.Object;

        _logger.Information("Subscription deleted - Subscription: {SubscriptionId}, Customer: {CustomerId}",
            subscription.Id, subscription.CustomerId);

        var subscriptionDeletedEvent = new StripeSubscriptionDeletedEvent(
            SubscriptionId: subscription.Id,
            CustomerId: subscription.CustomerId);

        StripeSubscriptionDeletedJob.Enqueue(subscriptionDeletedEvent);

        _logger.Information("Enqueued StripeSubscriptionDeletedEvent for subscription: {SubscriptionId}", subscription.Id);
    }
}
