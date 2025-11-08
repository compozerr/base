using Core.Abstractions;

namespace Database.DomainEventQueuers;

public class BeforeAndAfterSaveChangesDomainEventQueuer(List<IDomainEvent> events) : IDomainEventQueuer
{
    public void EnqueueEvent(IDomainEvent @event)
    {
        var beforeEvent = new DomainEventWithTriggerTiming(
            @event,
            DomainEventTriggerTiming.IsBeforeSaveChanges);

        var afterEvent = new DomainEventWithTriggerTiming(
            @event,
            DomainEventTriggerTiming.IsAfterSaveChanges);

        events.AddRange([beforeEvent, afterEvent]);
    }
}