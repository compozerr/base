using MediatR;

namespace Core.Abstractions;

/// <summary>
/// Base class for handling non-entity domain events (IDomainEvent but not IEntityDomainEvent).
/// Non-entity events are dispatched only ONCE after save completes.
///
/// Use this for simple notifications and side effects that:
/// - Don't need to modify entities
/// - Should only execute once after the transaction commits
/// - Don't depend on before/after timing
///
/// Examples: sending emails, logging, triggering external systems
/// </summary>
public abstract class DomainEventHandler<TEvent> : INotificationHandler<DomainEventEnvelope<TEvent>>
    where TEvent : IDomainEvent
{
    public async Task Handle(DomainEventEnvelope<TEvent> notification, CancellationToken cancellationToken)
    {
        if (notification.Timing == DomainEventTiming.AfterSaveChanges)
        {
            await HandleAsync(notification.InnerEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Handle the event after SaveChanges completes.
    /// Called exactly once when the event is dispatched.
    /// </summary>
    protected abstract Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
