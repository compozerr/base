using Api.Data;
using Api.Hosting.Jobs;
using Core.Abstractions;

namespace Api.EventHandlers.VMPooling;

public sealed class CreateDeployment_ProjectDelegatedEventHandler
    : EntityDomainEventHandlerBase<ProjectDelegatedEvent>
{
    protected override Task HandleAfterSaveAsync(
        ProjectDelegatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // Enqueue background job instead of making synchronous GitHub API call
        // This prevents the domain event handler from freezing during external API calls
        DeployFromLatestCommitJob.Enqueue(new DeployFromLatestCommitJobArgs(
            domainEvent.Entity.Id,
            OverrideAuthorization: false));

        return Task.CompletedTask;
    }
}
