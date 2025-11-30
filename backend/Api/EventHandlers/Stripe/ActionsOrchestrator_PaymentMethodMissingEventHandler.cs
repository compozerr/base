using Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;
using Auth.Abstractions;
using Auth.Repositories;
using Core.Abstractions;
using Core.Extensions;
using Stripe.Data.Repositories;
using Stripe.Events;

namespace Api.EventHandlers.Stripe;

public class PaymentMethodMissingEventHandler(
    IUserRepository userRepository,
    IStripeCustomerRepository stripeCustomerRepository,
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

        var userId = await stripeCustomerRepository.GetInternalIdByStripeCustomerIdAsync(
            notification.CustomerId,
            cancellationToken);

        if (!UserId.TryParse(userId, out var parsedUserId) || userId == null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("No internal user id found for Stripe Customer ID: {CustomerId}. Cannot send payment method missing notification",
                notification.CustomerId);
            return;
        }

        var user = await userRepository.GetByIdAsync(
            parsedUserId,
            cancellationToken);

        if (user == null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("User with ID: {UserId} not found. Cannot send payment method missing notification",
                parsedUserId);
            return;
        }

        var actionFactory = new PaymentMethodMissingActionFactory(
            notification,
            user,
            serviceProvider);

        var actions = actionFactory.CreateActions();

        await actions.ApplyAsync(a => a.ExecuteAsync(cancellationToken))
                     .LogAndSilenceAsync();
    }
}
