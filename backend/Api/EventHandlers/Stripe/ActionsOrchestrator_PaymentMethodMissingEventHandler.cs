using Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;
using Core.Abstractions;
using Core.Extensions;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class PaymentMethodMissingEventHandler(
    IStripeOrgResolver stripeOrgResolver,
    IServiceProvider serviceProvider) : IEventHandler<PaymentMethodMissingEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<PaymentMethodMissingEventHandler>();

    public async Task Handle(
        PaymentMethodMissingEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.Information("Processing payment method missing - Customer: {CustomerId}, Attempt: {AttemptCount}",
            notification.CustomerId,
            notification.AttemptCount);

        var resolved = await stripeOrgResolver.ResolveOrgAndOwnerByStripeCustomerIdAsync(notification.CustomerId, cancellationToken);
        if (resolved is null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("Cannot send payment method missing notification for Customer: {CustomerId}",
                notification.CustomerId);
            return;
        }

        var actionFactory = new PaymentMethodMissingActionFactory(
            notification,
            resolved.Owner,
            resolved.Organization,
            serviceProvider);

        var actions = actionFactory.CreateActions();

        await actions.ApplyAsync(a => a.ExecuteAsync(cancellationToken))
                     .LogAndSilenceAsync();
    }
}
