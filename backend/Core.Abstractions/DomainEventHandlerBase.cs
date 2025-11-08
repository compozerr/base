namespace Core.Abstractions;

public abstract class DomainEventHandlerBase<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    public Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        return notification is DomainEventWithTriggerTiming domainEventWithTiming
            ? domainEventWithTiming.When switch
            {
                DomainEventTriggerTiming.IsBeforeSaveChanges =>
                    HandleBeforeSaveChangesAsync(notification, cancellationToken),
                DomainEventTriggerTiming.IsAfterSaveChanges =>
                    HandleAfterSaveChangesAsync(notification, cancellationToken),
                _ => throw new NotSupportedException(
                    $"The specified domain event trigger timing '{domainEventWithTiming.When}' is not supported."),
            } : HandleAfterSaveChangesAsync(notification, cancellationToken);
    }

    public virtual Task HandleBeforeSaveChangesAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task HandleAfterSaveChangesAsync(TEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}