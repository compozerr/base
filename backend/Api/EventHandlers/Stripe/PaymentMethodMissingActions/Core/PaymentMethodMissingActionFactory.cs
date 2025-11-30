using Auth.Models;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;

public class PaymentMethodMissingActionFactory(
    PaymentMethodMissingEvent @event,
    User user,
    IServiceProvider serviceProvider)
{
    public IReadOnlyList<IPaymentMethodMissingAction> CreateActions()
    {
        return @event.AttemptCount switch
        {
            0 =>
            [
                new OnboardingMail_PaymentMethodMissingAction(@event, user, serviceProvider)
            ],
            1 =>
            [
                new FirstWarningMail_PaymentMethodMissingAction(@event, user, serviceProvider)
            ],
            2 =>
            [
                new SecondWarningMail_PaymentMethodMissingAction(@event, user, serviceProvider)
            ],
            3 =>
            [
                new ThirdAndTerminationMail_PaymentMethodMissingAction(@event, user, serviceProvider),
                new TerminateAllProjects_PaymentMethodMissingAction(@event, user, serviceProvider)
            ],
            _ => []
        };
    }
}