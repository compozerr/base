using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Core.Abstractions;
using Core.Extensions;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class PaymentFailedEventHandler(
    IStripeOrgResolver stripeOrgResolver,
    IServiceProvider serviceProvider) : IEventHandler<PaymentFailedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<PaymentFailedEventHandler>();

    public async Task Handle(
        PaymentFailedEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.Warning("Processing failed invoice payment - Invoice: {InvoiceId}, Customer: {CustomerId}, Amount: {AmountDue}, Attempt: {AttemptCount}",
            notification.InvoiceId,
            notification.CustomerId,
            notification.AmountDue,
            notification.AttemptCount);

        var resolved = await stripeOrgResolver.ResolveOrgAndOwnerByStripeCustomerIdAsync(notification.CustomerId, cancellationToken);
        if (resolved is null)
        {
            _logger.ForContext(nameof(notification), notification, true)
                   .Error("Cannot send failed payment notification for Invoice: {InvoiceId}",
                notification.InvoiceId);
            return;
        }

        var actionFactory = new PaymentFailedActionFactory(
            notification,
            resolved.Owner,
            serviceProvider);

        var actions = actionFactory.CreateActions();

        await actions.ApplyAsync(a => a.ExecuteAsync(cancellationToken))
                     .LogAndSilenceAsync();
    }
}
