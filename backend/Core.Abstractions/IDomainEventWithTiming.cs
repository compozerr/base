using MediatR;

namespace Core.Abstractions;

/// <summary>
/// Internal marker interface for domain events wrapped with timing information.
/// This is used by the infrastructure to dispatch events at the correct timing.
/// </summary>
public interface IDomainEventWithTiming : INotification
{
    DomainEventTiming Timing { get; }
}
