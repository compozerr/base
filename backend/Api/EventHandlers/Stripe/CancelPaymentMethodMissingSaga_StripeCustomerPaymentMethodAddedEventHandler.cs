using Core.Abstractions;
using Stripe.Data.Models;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class CancelPaymentMethodMissingSaga_StripeCustomerPaymentMethodAddedEventHandler(
    IPaymentMethodMissingSagaOrchestrator orchestrator) : IEventHandler<StripeCustomerPaymentMethodAddedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<CancelPaymentMethodMissingSaga_StripeCustomerPaymentMethodAddedEventHandler>();

    public async Task Handle(StripeCustomerPaymentMethodAddedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Information(
            "Payment method {PaymentMethodId} added for customer {CustomerId}, checking for active saga to cancel",
            notification.PaymentMethodId,
            notification.CustomerId);

        await orchestrator.CancelSagaAsync(
            notification.CustomerId,
            PaymentMethodMissingSagaCancellationReason.PaymentMethodAdded,
            cancellationToken);
    }
}
