using Api.Data.Events;
using Api.Hosting.Jobs;
using Core.Abstractions;

namespace Api.Features.N8n.EventHandlers;

public sealed class CreateDeployment_N8nProjectCreatedEventHandler
    : EntityDomainEventHandlerBase<N8nProjectCreatedEvent>
{
    protected override Task HandleAfterSaveAsync(
        N8nProjectCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // Enqueue background job instead of making synchronous GitHub API call
        // This prevents the domain event handler from freezing during external API calls
        DeployFromLatestCommitJob.Enqueue(new DeployFromLatestCommitJobArgs(
            domainEvent.Entity.Id,
            OverrideAuthorization: domainEvent.OverrideAuthorization));

        return Task.CompletedTask;
    }
}
