using Database.Events;

namespace Api.Data.Events;

public sealed record N8nProjectCreatedEvent(
    Project Entity, 
    bool OverrideAuthorization = false) : IEntityDomainEvent<Project>;