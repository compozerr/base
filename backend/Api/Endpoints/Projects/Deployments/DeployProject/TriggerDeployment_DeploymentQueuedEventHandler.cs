using Api.Abstractions;
using Core.Abstractions;
using Core.Extensions;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.DeployProject;

public sealed class TriggerDeployment_DeploymentQueuedEventHandler(
    IMediator mediator) : IDomainEventHandler<DeploymentQueuedEvent>
{
    public Task Handle(DeploymentQueuedEvent notification, CancellationToken cancellationToken)
    {
        Log.ForContext("DeploymentId", notification.Entity.Id)
           .Information("Deployment queued, triggering deployment");

        mediator.Send(
            new DeploymentTriggerCommand(
                notification.Entity.ProjectId),
            cancellationToken).LogAndSilence();

        return Task.CompletedTask;
    }
}
