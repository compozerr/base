using Api.Data.Repositories;
using Core.Abstractions;
using MediatR;
using Stripe.Events;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed class ChangeTier_StripeSubscriptionUpdatedEventHandler(
    ISender sender,
    IDeploymentRepository deploymentRepository) : IEventHandler<StripeSubscriptionUpdatedEvent>
{
    public async Task Handle(
        StripeSubscriptionUpdatedEvent @event,
        CancellationToken cancellationToken)
    {
        // Skip tier change for new projects (no deployments yet)
        // The tier is already set correctly during project creation,
        // and attempting to change it would cause a nested SaveChangesAsync deadlock
        var currentDeploymentId = await deploymentRepository.GetCurrentDeploymentId(@event.ProjectId);
        if (currentDeploymentId is null)
        {
            return;
        }

        var command = new ChangeTierCommand(
            @event.ProjectId,
            @event.ServerTierId.Value);

        await sender.Send(command, cancellationToken);
    }
}