using Core.Abstractions;

namespace Database.DomainEventQueuers;

public class SingleInstanceDomainEventQueuer(List<IDomainEvent> events) : IDomainEventQueuer
{
    public void EnqueueEvent(IDomainEvent @event)
    {
        events.Add(@event);
    }
}