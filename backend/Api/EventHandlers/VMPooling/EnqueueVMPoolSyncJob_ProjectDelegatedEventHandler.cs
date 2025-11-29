using Api.Data;
using Api.Hosting.VMPooling.Jobs;
using Core.Abstractions;

namespace Api.EventHandlers.VMPooling;

public sealed class EnqueueVMPoolSyncJob_ProjectDelegatedEventHandler
    : EntityDomainEventHandlerBase<ProjectDelegatedEvent>
{
    protected override async Task HandleAfterSaveAsync(
        ProjectDelegatedEvent domainEvent,
        CancellationToken cancellationToken)
     => VMPoolSyncJob.Enqueue();
}
