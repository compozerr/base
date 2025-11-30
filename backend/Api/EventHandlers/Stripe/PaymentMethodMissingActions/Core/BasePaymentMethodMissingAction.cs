using Auth.Models;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;

public interface IPaymentMethodMissingAction
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public abstract class BasePaymentMethodMissingAction(
    PaymentMethodMissingEvent @event,
    User user,
    IServiceProvider serviceProvider) : IPaymentMethodMissingAction
{
    protected readonly PaymentMethodMissingEvent Event = @event;
    protected readonly User User = user;
    protected readonly IServiceProvider ServiceProvider = serviceProvider;

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}