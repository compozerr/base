using Api.Data;
using Database.Events;

namespace Cli.Endpoints.Projects;

public sealed record ProjectCreatedEvent(
    Project Entity) : IEntityDomainEvent<Project>;