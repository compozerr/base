using Core.Abstractions;

namespace Database.Events;

public interface IEntityDomainEvent<out T> : IDomainEvent
    where T : BaseEntity
{
    T Entity { get; }
}