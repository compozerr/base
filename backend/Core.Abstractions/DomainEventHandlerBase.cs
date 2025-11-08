using MediatR;

namespace Core.Abstractions;

/// <summary>
/// Base class for handling entity domain events (IEntityDomainEvent).
/// Entity events are automatically dispatched at BOTH before and after save timings.
/// Override HandleBeforeSaveAsync and/or HandleAfterSaveAsync for the timings you need.
///
/// Use this for events tied to entities that may need to:
/// - Modify related entities before save (HandleBeforeSaveAsync)
/// - Perform side effects after save with IDs available (HandleAfterSaveAsync)
/// </summary>
public abstract class EntityDomainEventHandlerBase<TEvent> : INotificationHandler<DomainEventEnvelope<TEvent>>
    where TEvent : IDomainEvent
{
    public async Task Handle(DomainEventEnvelope<TEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.InnerEvent;

        switch (notification.Timing)
        {
            case DomainEventTiming.BeforeSaveChanges:
                await HandleBeforeSaveAsync(domainEvent, cancellationToken);
                break;

            case DomainEventTiming.AfterSaveChanges:
                await HandleAfterSaveAsync(domainEvent, cancellationToken);
                break;

            default:
                throw new NotSupportedException(
                    $"The specified domain event timing '{notification.Timing}' is not supported.");
        }
    }

    /// <summary>
    /// Handle the event BEFORE SaveChanges is called.
    /// Use this when you need to modify other entities in the same transaction.
    /// Changes made here will be included in the same SaveChanges call.
    /// </summary>
    protected virtual Task HandleBeforeSaveAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle the event AFTER SaveChanges completes successfully.
    /// Use this when you need entity IDs (which are generated during save) or want to perform external operations.
    /// The database transaction has been committed at this point.
    /// </summary>
    protected virtual Task HandleAfterSaveAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
