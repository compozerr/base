using Api.Data;
using Database.Events;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed record ExternalDomainAddedEvent(
    Domain Entity) : IEntityDomainEvent<Domain>;
