using Api.Data;
using Api.Data.Repositories;
using Core.Abstractions;

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

        // CRITICAL: BeforeSave handlers must NOT call SaveChanges!
        // Just modify entities in memory - SaveChanges happens after all handlers complete
        foreach (var vmPoolItem in vmPoolItems)
        {
            vmPoolItem.DelegatedAt = DateTime.UtcNow;
            vmPoolItem.DelegatedToUserId = domainEvent.Entity.UserId;
        }
    }
}
