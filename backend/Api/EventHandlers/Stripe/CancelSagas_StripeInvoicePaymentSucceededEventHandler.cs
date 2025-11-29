using Core.Abstractions;
using Stripe.Data.Models;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class CancelSagas_StripeInvoicePaymentSucceededEventHandler(
    IPaymentFailureSagaOrchestrator paymentFailureSagaOrchestrator) : IEventHandler<StripeInvoicePaymentSucceededEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<CancelSagas_StripeInvoicePaymentSucceededEventHandler>();

    public async Task Handle(
        StripeInvoicePaymentSucceededEvent notification,
        CancellationToken cancellationToken)
    {
        await paymentFailureSagaOrchestrator.CancelSagaAsync(notification.SubscriptionId,
                        PaymentFailureSagaCancellationReason.PaymentSucceeded,
                        cancellationToken);

        _logger.Information(
            "Cancelled payment failure sagas for Subscription: {SubscriptionId} due to successful payment of Invoice: {InvoiceId}",
            notification.SubscriptionId,
            notification.InvoiceId);
    }
}