using Database.Events;

namespace Database.Extensions;

public static class BaseEntityExtensions
{
    /// <summary>
    /// Queues a domain event on the entity by creating an instance of the event type.
    /// The event must have a constructor that takes the entity as a parameter.
    /// The timing (before/after save) is determined by the marker interfaces on the event type.
    /// </summary>
    public static void QueueDomainEvent<TEvent>(this BaseEntity entity)
        where TEvent : IEntityDomainEvent<BaseEntity>
    {
        var domainEvent = (TEvent)Activator.CreateInstance(typeof(TEvent), entity)!;
        entity.QueueDomainEvent(domainEvent);
    }

    /// <summary>
    /// Queues a pre-constructed domain event on the entity.
    /// The timing (before/after save) is determined by the marker interfaces on the event instance.
    /// </summary>
    public static void QueueDomainEvent<TEvent>(this BaseEntity entity, TEvent domainEvent)
        where TEvent : IEntityDomainEvent<BaseEntity>
    {
        entity.QueueDomainEvent(domainEvent);
    }
}
