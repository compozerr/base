using Auth.Models;
using Organizations.Models;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;

public class PaymentMethodMissingActionFactory(
    PaymentMethodMissingEvent @event,
    User user,
    Organization organization,
    IServiceProvider serviceProvider)
{
    public IReadOnlyList<IPaymentMethodMissingAction> CreateActions()
    {
        return @event.AttemptCount switch
        {
            0 =>
            [
                new OnboardingMail_PaymentMethodMissingAction(@event, user, organization, serviceProvider)
            ],
            1 =>
            [
                new FirstWarningMail_PaymentMethodMissingAction(@event, user, organization, serviceProvider)
            ],
            2 =>
            [
                new SecondWarningMail_PaymentMethodMissingAction(@event, user, organization, serviceProvider)
            ],
            3 =>
            [
                new ThirdAndTerminationMail_PaymentMethodMissingAction(@event, user, organization, serviceProvider),
                new TerminateAllProjects_PaymentMethodMissingAction(@event, user, organization, serviceProvider)
            ],
            _ => []
        };
    }
}