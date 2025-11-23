using Api.Endpoints.Projects.Project.Delete;
using Api.EventHandlers.Stripe.PaymentFailedActions.Core;
using Auth.Models;
using MediatR;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe.PaymentFailedActions;

public sealed class TerminateProject_PaymentFailedAction(
    StripeInvoicePaymentFailedEvent @event,
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

        var subscriptions = await subscriptionsService.GetSubscriptionsForUserAsync(User.Id.Value.ToString(), cancellationToken);

        if (subscriptions is null || !subscriptions.Any())
        {
            _logger.Error("No subscriptions found for user. Cannot cancel subscription for project termination. Invoice: {InvoiceId}", Event.InvoiceId);
            return; // No subscriptions to cancel
        }

        var projectId = subscriptions.Where(x => x.Id == Event.SubscriptionId).FirstOrDefault()?.ProjectId;

        if (projectId is null)
        {
            _logger.Error("No project ID found for subscription. Cannot cancel subscription for project termination. Invoice: {InvoiceId}", Event.InvoiceId);
            return; // No project ID found
        }

        try
        {
            await sender.Send(new DeleteProjectCommand(
                 ProjectId: projectId,
                 SkipOwnerCheck: true
             ), cancellationToken);
            _logger.Information("Successfully sent terminated project for invoice: {InvoiceId}", Event.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling terminated project for invoice: {InvoiceId}", Event.InvoiceId);
            throw;
        }
    }
}