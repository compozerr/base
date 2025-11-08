namespace Core.Abstractions;

/// <summary>
/// Wrapper for domain events to track when they should be dispatched.
/// This allows a single event to be dispatched at multiple timings if needed.
/// The envelope inherits from the actual event type so MediatR can route it to the correct handlers.
/// </summary>
public sealed class DomainEventEnvelope<TEvent> : IDomainEventWithTiming
    where TEvent : IDomainEvent
{
    private readonly TEvent _innerEvent;

    public DomainEventEnvelope(TEvent innerEvent, DomainEventTiming timing)
    {
        _innerEvent = innerEvent;
        Timing = timing;
    }

    public TEvent InnerEvent => _innerEvent;
    public DomainEventTiming Timing { get; }

    public static implicit operator TEvent(DomainEventEnvelope<TEvent> envelope)
        => envelope._innerEvent;
}
