using Database.Events;

namespace Api.Data.Events;

public sealed record N8nProjectCreatedEvent(
    Project Entity) : IEntityDomainEvent<Project>;