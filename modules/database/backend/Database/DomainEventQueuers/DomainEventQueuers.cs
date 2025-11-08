namespace Database.DomainEventQueuers;

public enum DomainEventQueuerTypes
{
    SingleInstance = 1,
    BeforeAndAfterSaveChanges = 2
}