using Auth.Models;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentFailedActions.Core;

public interface IPaymentFailedAction
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public abstract class BasePaymentFailedAction(
    PaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider) : IPaymentFailedAction
{
    protected readonly PaymentFailedEvent Event = @event;
    protected readonly User User = user;
    protected readonly IServiceProvider ServiceProvider = serviceProvider;

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}