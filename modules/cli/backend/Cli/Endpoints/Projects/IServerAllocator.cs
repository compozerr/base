using Api.Abstractions;
using Api.Data.Repositories;
using Api.Data.Services;
using Serilog;

namespace Cli.Endpoints.Projects;

public sealed record ServerAllocationResult(ServerId ServerId, LocationId? ChangedToLocationId);

public interface IServerAllocator
{
    Task<ServerAllocationResult> AllocateForLocationAsync(LocationId locationId, CancellationToken cancellationToken = default);
}

public sealed class ServerAllocator(
    IServerRepository serverRepository,
    IServerVisibilityFilter serverVisibilityFilter) : IServerAllocator
{
    public async Task<ServerAllocationResult> AllocateForLocationAsync(LocationId locationId, CancellationToken cancellationToken = default)
    {
        var serversInLocation = await serverRepository.GetServersByLocationId(locationId);
        var visibleServersInLocation = serverVisibilityFilter.ApplyForCurrentUser(serversInLocation).ToList();

        if (visibleServersInLocation.Count == 0)
        {
            Log.ForContext(nameof(locationId), locationId)
               .Fatal("No servers visible to current user on given locationId, falling back to first available across all locations");

            var allServers = await serverRepository.GetAllAsync();
            var firstServer = serverVisibilityFilter.ApplyForCurrentUser(allServers).First();

            return new ServerAllocationResult(firstServer.Id, firstServer.LocationId);
        }

        var firstServerOnLocation = visibleServersInLocation.OrderBy(x => x.Usage.AvgRamPercentage).First();

        return new ServerAllocationResult(firstServerOnLocation.Id, null);
    }
}
