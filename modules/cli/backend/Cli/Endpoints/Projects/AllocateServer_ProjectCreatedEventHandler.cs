using Api.Abstractions;
using Api.Data.Repositories;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class AllocateServer_ProjectCreatedEventHandler(
    IServerRepository serverRepository) : IDomainEventHandler<ProjectCreatedEvent>
{
    public async Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        notification.Entity.ServerId = await GetBestServerInLocationAsync(notification.Entity.LocationId);
    }

    private async Task<ServerId> GetBestServerInLocationAsync(LocationId locationId)
    {
        // TODO: Implement better load balancing when more servers available
        var serversOnLocation = await serverRepository.GetServersByLocationId(locationId);

        if (serversOnLocation.Count == 0)
        {
            Log.ForContext(nameof(locationId), locationId)
               .Fatal("No servers on given locationId, just giving the first server available");

            return (await serverRepository.GetAllAsync()).First().Id;
        }

        return serversOnLocation.First().Id;
    }
}