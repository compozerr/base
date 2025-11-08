using Core.Abstractions;

namespace Database.DomainEventQueuers;

public static class DomainEventQueuerFactory
{
    public static IDomainEventQueuer Create(
        DomainEventQueuerTypes queuerType,
        List<IDomainEvent> events)
    {
        return queuerType switch
        {
            DomainEventQueuerTypes.SingleInstance => new SingleInstanceDomainEventQueuer(events),
            DomainEventQueuerTypes.BeforeAndAfterSaveChanges => new BeforeAndAfterSaveChangesDomainEventQueuer(events),
            _ => throw new NotSupportedException($"The specified event queuer type '{queuerType}' is not supported."),
        };
    }
}