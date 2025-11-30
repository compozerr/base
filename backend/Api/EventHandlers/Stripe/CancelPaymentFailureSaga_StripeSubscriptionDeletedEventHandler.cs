using Core.Abstractions;
using Stripe.Data.Models;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class CancelPaymentFailureSaga_StripeSubscriptionDeletedEventHandler(
    IPaymentFailureSagaOrchestrator orchestrator) : IEventHandler<StripeSubscriptionDeletedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<CancelPaymentFailureSaga_StripeSubscriptionDeletedEventHandler>();

    public async Task Handle(StripeSubscriptionDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Information(
            "Subscription {SubscriptionId} cancelled for customer {CustomerId}, checking for active saga to cancel",
            notification.SubscriptionId,
            notification.CustomerId);

        await orchestrator.CancelSagaAsync(
            notification.SubscriptionId,
            PaymentFailureSagaCancellationReason.SubscriptionCancelled,
            cancellationToken);
    }
}
