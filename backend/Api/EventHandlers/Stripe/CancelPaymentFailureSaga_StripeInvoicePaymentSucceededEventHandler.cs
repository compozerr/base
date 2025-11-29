using Core.Abstractions;
using Stripe.Data.Models;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class CancelPaymentFailureSaga_StripeInvoicePaymentSucceededEventHandler(
    IPaymentFailureSagaOrchestrator orchestrator) : IEventHandler<StripeInvoicePaymentSucceededEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<CancelPaymentFailureSaga_StripeInvoicePaymentSucceededEventHandler>();

    public async Task Handle(StripeInvoicePaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        _logger.Information(
            "Payment succeeded for invoice {InvoiceId}, subscription {SubscriptionId}, checking for active saga to cancel",
            notification.InvoiceId,
            notification.SubscriptionId);

        await orchestrator.CancelSagaAsync(
            notification.SubscriptionId,
            PaymentFailureSagaCancellationReason.PaymentSucceeded,
            cancellationToken);
    }
}
