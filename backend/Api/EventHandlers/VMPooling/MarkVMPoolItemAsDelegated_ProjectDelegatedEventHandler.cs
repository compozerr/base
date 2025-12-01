using Api.Data;
using Api.Data.Repositories;
using Core.Abstractions;
using Core.Extensions;

namespace Api.EventHandlers.VMPooling;

public sealed class MarkVMPoolItemAsDelegated_ProjectDelegatedEventHandler(
    IVMPoolItemRepository vMPoolItemRepository)
    : EntityDomainEventHandlerBase<ProjectDelegatedEvent>
{
    private readonly Serilog.ILogger _logger = Log.ForContext<MarkVMPoolItemAsDelegated_ProjectDelegatedEventHandler>();
    
    protected override async Task HandleBeforeSaveAsync(
        ProjectDelegatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var vmPoolItems = await vMPoolItemRepository.GetFilteredAsync(
            x => x.ProjectId == domainEvent.Entity.Id,
            cancellationToken: cancellationToken);

        if (vmPoolItems == null || vmPoolItems.Count == 0)
        {
            _logger.Warning("No VMPoolItem found for Project ID {ProjectId} when handling ProjectDelegatedEvent", domainEvent.Entity.Id);
            return;
        }

        await vmPoolItems.ApplyAsync(
            async (vmPoolItem)
                => await vMPoolItemRepository.MarkAsDelegatedAsync(
                    vmPoolItem,
                    domainEvent.Entity.UserId,
                    DateTime.UtcNow));
    }
}
