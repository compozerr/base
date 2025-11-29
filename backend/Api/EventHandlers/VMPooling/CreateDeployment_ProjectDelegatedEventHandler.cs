using Api.Data;
using Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;
using Core.Abstractions;
using MediatR;

namespace Api.EventHandlers.VMPooling;

public sealed class CreateDeployment_ProjectDelegatedEventHandler(
    ISender sender)
    : EntityDomainEventHandlerBase<ProjectDelegatedEvent>
{
    protected override async Task HandleAfterSaveAsync(
        ProjectDelegatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var deployFromLatestCommitCommand = new DeployFromLatestCommitCommand(
            domainEvent.Entity.Id);

        await sender.Send(
            deployFromLatestCommitCommand,
            cancellationToken);
    }
}
