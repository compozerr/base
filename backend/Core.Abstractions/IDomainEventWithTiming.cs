namespace Core.Abstractions;

/// <summary>
/// Internal marker interface for domain events wrapped with timing information.
/// This is used by the infrastructure to dispatch events at the correct timing.
/// </summary>
public interface IDomainEventWithTiming : IDomainEvent
{
    DomainEventTiming Timing { get; }
}
