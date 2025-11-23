using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Abstractions;
using Auth.Repositories;
using Core.Abstractions;
using Core.Extensions;
using Stripe.Data.Repositories;
using Stripe.Events;

namespace Api.EventHandlers.Stripe;

public class StripeInvoicePaymentFailedEventHandler(
    IUserRepository userRepository,
    IStripeCustomerRepository stripeCustomerRepository,
    IServiceProvider serviceProvider) : IEventHandler<StripeInvoicePaymentFailedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<StripeInvoicePaymentFailedEventHandler>();

    public async Task Handle(
        StripeInvoicePaymentFailedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.Warning("Processing failed invoice payment - Invoice: {InvoiceId}, Customer: {CustomerId}, Amount: {AmountDue}, Attempt: {AttemptCount}",
            notification.InvoiceId,
            notification.CustomerId,
            notification.AmountDue,
            notification.AttemptCount);

        var userId = await stripeCustomerRepository.GetInternalIdByStripeCustomerIdAsync(
            notification.CustomerId,
            cancellationToken);

        if (!UserId.TryParse(userId, out var parsedUserId) || userId == null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("No internal user id found for Stripe Customer ID: {CustomerId}. Cannot send failed payment notification for Invoice: {InvoiceId}",
                notification.CustomerId,
                notification.InvoiceId);
            return;
        }

        var user = await userRepository.GetByIdAsync(
            parsedUserId,
            cancellationToken);

        if (user == null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("User with ID: {UserId} not found. Cannot send failed payment notification for Invoice: {InvoiceId}",
                parsedUserId,
                notification.InvoiceId);
            return;
        }

        var actionFactory = new PaymentFailedActionFactory(
            notification,
            user,
            serviceProvider);

        var actions = actionFactory.CreateActions();

        await actions.ApplyAsync(a => a.ExecuteAsync(cancellationToken))
                     .LogAndSilenceAsync();
    }
}