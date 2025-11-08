using Api.Data;
using Database.Events;

namespace Cli.Abstractions;

public sealed record ProjectCreatedEvent(
    Project Entity) : IEntityDomainEvent<Project>;
