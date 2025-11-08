using Api.Data;
using Database.Events;

namespace Api.Features.N8n.Events;

public sealed record N8nProjectCreatedEvent(
    Project Entity) : IEntityDomainEvent<Project>;