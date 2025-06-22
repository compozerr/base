using Api.Data;
using Database.Events;

namespace Cli.Endpoints.Projects;

public sealed record ProjectCreatedEvent_AfterSave(
    Project Entity) : IEntityDomainEvent<Project>;