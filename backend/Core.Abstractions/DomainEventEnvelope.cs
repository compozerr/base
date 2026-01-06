namespace Core.Abstractions;

/// <summary>
/// Wrapper for domain events to track when they should be dispatched.
/// This allows a single event to be dispatched at multiple timings if needed.
/// The envelope inherits from the actual event type so MediatR can route it to the correct handlers.
/// </summary>
public sealed class DomainEventEnvelope<TEvent>(TEvent innerEvent, DomainEventTiming timing) : IDomainEventWithTiming
    where TEvent : IDomainEvent
{
    private readonly TEvent _innerEvent = innerEvent;

    public TEvent InnerEvent => _innerEvent;
    public DomainEventTiming Timing { get; } = timing;

    public static implicit operator TEvent(DomainEventEnvelope<TEvent> envelope)
        => envelope._innerEvent;
}
