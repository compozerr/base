using Auth.Models;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentFailedActions.Core;

public class PaymentFailedActionFactory(
    StripeInvoicePaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider)
{
    public IReadOnlyList<IPaymentFailedAction> CreateActions()
    {
        return [];
    }
}