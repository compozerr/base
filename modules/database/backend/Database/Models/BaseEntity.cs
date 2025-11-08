using System.ComponentModel.DataAnnotations.Schema;
using Core.Abstractions;
using Database.DomainEventQueuers;

namespace Database.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public bool IsDeleted => DeletedAtUtc != null;

    [NotMapped]
    public List<IDomainEvent> DomainEvents { get; } = [];

    public void QueueDomainEvent(
        IDomainEvent domainEvent,
        DomainEventQueuerTypes domainEventQueuersTypes)
    {
        var eventQueuer = DomainEventQueuerFactory.Create(
            domainEventQueuersTypes,
            DomainEvents);

        eventQueuer.EnqueueEvent(domainEvent);
    }
}

public abstract class BaseEntityWithId<TId> : BaseEntity where TId : IdBase<TId>, IId<TId>
{
    public TId Id { get; set; } = TId.Create(Guid.NewGuid());
}
