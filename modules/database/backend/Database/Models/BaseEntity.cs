using System.ComponentModel.DataAnnotations.Schema;
using Core.Abstractions;

namespace Database.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    [NotMapped]
    public List<IDomainEvent> DomainEvents { get; } = [];

    public void QueueDomainEvent(IDomainEvent domainEvent)
      => DomainEvents.Add(domainEvent);
}

public abstract class BaseEntityWithId<TId> : BaseEntity where TId : IdBase<TId>, IId<TId>
{
    public TId Id { get; set; } = TId.Create(Guid.NewGuid());
}
