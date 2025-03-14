using Api.Abstractions.Exceptions;
using Api.Hosting.Services;
using Core.Abstractions;

namespace Cli.Endpoints.Projects.Deployments;

public sealed class StartDeployment_DeploymentQueuedEventHandler(
    IHostingApiFactory hostingApiFactory) : IDomainEventHandler<DeploymentQueuedEvent>
{
    public async Task Handle(DeploymentQueuedEvent notification, CancellationToken cancellationToken)
    {
        var api = await hostingApiFactory.GetHostingApiAsync(notification.Entity.Project?.ServerId ?? throw new ServerNotFoundException());

        await api.DeployAsync(notification.Entity);
    }
}
