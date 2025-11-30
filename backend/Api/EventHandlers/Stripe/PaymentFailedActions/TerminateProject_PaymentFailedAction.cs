using Api.Endpoints.Projects.Project.Delete;
using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Models;
using MediatR;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe.PaymentFailedActions;

public sealed class TerminateProject_PaymentFailedAction(
    PaymentFailedEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentFailedAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<TerminateProject_PaymentFailedAction>();

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var subscriptionsService = scope.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            // Get the specific subscription for this failed payment saga
            var subscription = await subscriptionsService.GetSubscriptionByStripeSubscriptionIdAsync(
                Event.SubscriptionId,
                cancellationToken);

            if (subscription is null)
            {
                _logger.Warning(
                    "No subscription found for Stripe subscription ID {SubscriptionId}. Cannot terminate project for invoice: {InvoiceId}",
                    Event.SubscriptionId,
                    Event.InvoiceId);
                return;
            }

            if (subscription.ProjectId is null)
            {
                _logger.Warning(
                    "Subscription {SubscriptionId} has no associated project ID. Cannot terminate for invoice: {InvoiceId}",
                    Event.SubscriptionId,
                    Event.InvoiceId);
                return;
            }

            _logger.Information(
                "Terminating project {ProjectId} for unpaid invoice {InvoiceId} (subscription {SubscriptionId})",
                subscription.ProjectId,
                Event.InvoiceId,
                Event.SubscriptionId);

            await sender.Send(new DeleteProjectCommand(
                ProjectId: subscription.ProjectId,
                SkipOwnerCheck: true
            ), cancellationToken);

            _logger.Information(
                "Successfully terminated project {ProjectId} for unpaid invoice {InvoiceId}",
                subscription.ProjectId,
                Event.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Error terminating project for invoice {InvoiceId} (subscription {SubscriptionId})",
                Event.InvoiceId,
                Event.SubscriptionId);
            throw;
        }
    }
}
