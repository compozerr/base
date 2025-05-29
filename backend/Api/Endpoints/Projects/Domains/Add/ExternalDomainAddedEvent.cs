using Api.Data;
using Core.MediatR;
using Database.Events;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed record ExternalDomainAddedEvent(
    Domain Entity) : IEntityDomainEvent<Domain>, IDispatchBeforeSaveChanges;
