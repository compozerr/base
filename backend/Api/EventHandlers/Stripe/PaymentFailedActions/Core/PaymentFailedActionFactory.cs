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
        return @event.AttemptCount switch
        {
            1 =>
            [
                new FirstWarningMail_PaymentFailedAction(@event, user, serviceProvider)
            ],
            2 =>
            [
                new SecondWarningMail_PaymentFailedAction(@event, user, serviceProvider)
            ],
            3 =>
            [
                new ThirdAndTerminationMail_PaymentFailedAction(@event, user, serviceProvider),
                new TerminateProject_PaymentFailedAction(@event, user, serviceProvider)
            ],
            _ => []
        };
    }
}