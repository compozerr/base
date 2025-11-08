using Core.Abstractions;

namespace Database.DomainEventQueuers;

public interface IDomainEventQueuer
{
    void EnqueueEvent(IDomainEvent @event);
}