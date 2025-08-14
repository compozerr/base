using Core.Abstractions;
using Stripe.Events;

namespace Api.EventHandlers.Stripe;

public class StripeInvoicePaymentFailedEventHandler() : IEventHandler<StripeInvoicePaymentFailedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<StripeInvoicePaymentFailedEventHandler>();

    public async Task Handle(StripeInvoicePaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Warning("Processing failed invoice payment - Invoice: {InvoiceId}, Customer: {CustomerId}, Amount: {AmountDue}, Attempt: {AttemptCount}",
            notification.InvoiceId,
            notification.CustomerId,
            notification.AmountDue,
            notification.AttemptCount);

        try
        {

        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling failed payment for invoice: {InvoiceId}", notification.InvoiceId);
            throw;
        }
    }
}
