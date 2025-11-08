using Api.Abstractions;
using Core.Abstractions;
using Core.Extensions;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.DeployProject;

public sealed class TriggerDeployment_DeploymentQueuedEventHandler(
    IMediator mediator) : DomainEventHandlerBase<DeploymentQueuedEvent>
{
    public override async Task HandleBeforeSaveChangesAsync(DeploymentQueuedEvent domainEvent, CancellationToken cancellationToken)
    {
        Log.ForContext("DeploymentId", domainEvent.Entity.Id)
          .Information("Deployment queued, triggering deployment");

        await mediator.Send(
            new DeploymentTriggerCommand(
                domainEvent.Entity.ProjectId),
            cancellationToken);
    }
}
