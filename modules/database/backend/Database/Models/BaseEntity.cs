using System.ComponentModel.DataAnnotations.Schema;
using Core.Abstractions;
using Database.Events;

namespace Database.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public bool IsDeleted => DeletedAtUtc != null;

    [NotMapped]
    public List<IDomainEvent> DomainEvents { get; } = [];

    /// <summary>
    /// Queues a domain event to be dispatched.
    ///
    /// Timing behavior:
    /// - IDispatchBeforeSaveChanges: Dispatched at BOTH before AND after save
    ///   (handlers can implement HandleBeforeSaveAsync and/or HandleAfterSaveAsync)
    ///
    /// - IDispatchAfterSaveChanges: Dispatched ONLY after save
    ///   (handlers receive event once after save completes)
    ///
    /// - Neither marker: Dispatched ONLY after save (default)
    ///   (handlers receive event once after save completes)
    /// </summary>
    public void QueueDomainEvent(IDomainEvent domainEvent)
    {
        // If event has IDispatchBeforeSaveChanges, it will be dispatched at BOTH timings
        // Otherwise, it's dispatched once at the appropriate timing
        DomainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all queued domain events.
    /// This is called automatically by BaseDbContext after dispatching events.
    /// </summary>
    public void ClearDomainEvents()
    {
        DomainEvents.Clear();
    }
}

public abstract class BaseEntityWithId<TId> : BaseEntity where TId : IdBase<TId>, IId<TId>
{
    public TId Id { get; set; } = TId.Create(Guid.NewGuid());
}
