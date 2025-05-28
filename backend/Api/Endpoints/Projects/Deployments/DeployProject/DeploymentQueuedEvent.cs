using Api.Data;
using Database.Events;

namespace Api.Endpoints.Projects.Deployments.DeployProject;

public sealed record DeploymentQueuedEvent(
    Deployment Entity) : IEntityDomainEvent<Deployment>;