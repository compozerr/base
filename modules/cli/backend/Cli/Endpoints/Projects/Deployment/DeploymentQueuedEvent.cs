using Database.Events;

namespace Cli.Endpoints.Projects.Deployment;

public sealed record DeploymentQueuedEvent(
    Api.Data.Deployment Entity) : IEntityDomainEvent<Api.Data.Deployment>;