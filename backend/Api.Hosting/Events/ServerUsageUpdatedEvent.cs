using Api.Data;
using Database.Events;

namespace Api.Hosting.Events;

public sealed record ServerUsageUpdatedEvent(Server Entity) : IEntityDomainEvent<Server>;
