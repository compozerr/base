using Database.DomainEventQueuers;
using Database.Events;

namespace Database.Extensions;

public static class BaseEntityExtensions
{
    public static void QueueDomainEvent<TEvent>(this BaseEntity entity)
        where TEvent : IEntityDomainEvent<BaseEntity>
    {
        var domainEvent = (TEvent)Activator.CreateInstance(typeof(TEvent), entity)!;
        entity.QueueDomainEvent(
            domainEvent,
            DomainEventQueuerTypes.BeforeAndAfterSaveChanges);
    }
}
