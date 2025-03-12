using Api.Data;
using Database.Events;

namespace Cli.Endpoints.Projects.Deployments;

public sealed record DeploymentQueuedEvent(
    Deployment Entity) : IEntityDomainEvent<Deployment>;