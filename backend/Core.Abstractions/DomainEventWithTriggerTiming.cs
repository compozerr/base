namespace Core.Abstractions;

public class DomainEventWithTriggerTiming(
    IDomainEvent domainEvent,
    DomainEventTriggerTiming when) : IDomainEvent
{
    public IDomainEvent DomainEvent { get; } = domainEvent;
    public DomainEventTriggerTiming When { get; } = when;

}