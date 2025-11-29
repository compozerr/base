using Database.Events;

namespace Api.Data;

public sealed record ProjectDelegatedEvent(
    Project Entity) : IEntityDomainEvent<Project>;