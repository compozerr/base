using Api.Hosting.Services;
using Core.Abstractions;

namespace Cli.Endpoints.Projects.Deployments;

public sealed class StartDeployment_DeploymentQueuedEventHandler(
    IHostingServerHttpClientFactory hostingServerHttpClientFactory) : IDomainEventHandler<DeploymentQueuedEvent>
{
    public Task Handle(DeploymentQueuedEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}