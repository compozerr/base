using Api.Endpoints.Projects.Project.Delete;
using Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;
using Auth.Models;
using MediatR;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions;

public sealed class TerminateAllProjects_PaymentMethodMissingAction(
    PaymentMethodMissingEvent @event,
    User user,
    IServiceProvider serviceProvider
) : BasePaymentMethodMissingAction(@event, user, serviceProvider)
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<TerminateAllProjects_PaymentMethodMissingAction>();
    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        using var scope = ServiceProvider.CreateScope();
        var subscriptionsService = scope.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var subscriptions = await subscriptionsService.GetSubscriptionsForUserAsync(User.Id.Value.ToString(), cancellationToken);

        if (subscriptions is null || !subscriptions.Any())
        {
            _logger.Warning("No subscriptions found for user {UserId}. No projects to terminate for customer: {CustomerId}", User.Id, Event.CustomerId);
            return; // No subscriptions to cancel
        }

        var projectIds = subscriptions.Select(x => x.ProjectId).Distinct().ToList();

        _logger.Information("Terminating {ProjectCount} projects for customer: {CustomerId}", projectIds.Count, Event.CustomerId);

        try
        {
            foreach (var projectId in projectIds)
            {
                if (projectId is null)
                {
                    _logger.Warning("Skipping null project ID for customer: {CustomerId}", Event.CustomerId);
                    continue;
                }

                await sender.Send(new DeleteProjectCommand(
                     ProjectId: projectId,
                     SkipOwnerCheck: true
                 ), cancellationToken);

                _logger.Information("Successfully terminated project {ProjectId} for customer: {CustomerId}", projectId, Event.CustomerId);
            }

            _logger.Information("Successfully terminated all {ProjectCount} projects for customer: {CustomerId}", projectIds.Count, Event.CustomerId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error terminating projects for customer: {CustomerId}", Event.CustomerId);
            throw;
        }
    }
}