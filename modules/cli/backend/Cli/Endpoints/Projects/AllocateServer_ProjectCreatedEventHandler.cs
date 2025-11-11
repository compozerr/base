using Api.Abstractions;
using Api.Data.Repositories;
using Cli.Abstractions;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class AllocateServer_ProjectCreatedEventHandler(
    IServerRepository serverRepository) : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override async Task HandleBeforeSaveAsync(ProjectCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var bestServerInLocationResponse = await GetBestServerInLocationAsync(domainEvent.Entity.LocationId);
        domainEvent.Entity.ServerId = bestServerInLocationResponse.ServerId;

        if (bestServerInLocationResponse.ChangedToLocationId is { } changedToLocationId)
            domainEvent.Entity.LocationId = changedToLocationId;
    }

    private sealed record BestServerInLocationResponse(ServerId ServerId, LocationId? ChangedToLocationId);

    private async Task<BestServerInLocationResponse> GetBestServerInLocationAsync(LocationId locationId)
    {
        var serversOnLocation = (await serverRepository.GetServersByLocationId(locationId)).Where(x => x.ServerVisibility == Api.Data.ServerVisibility.Public).ToList();

        if (serversOnLocation.Count == 0)
        {
            Log.ForContext(nameof(locationId), locationId)
               .Fatal("No servers on given locationId, just giving the first server available");

            var firstServer = (await serverRepository.GetAllAsync()).First(x => x.ServerVisibility == Api.Data.ServerVisibility.Public);

            return new(firstServer.Id, firstServer.LocationId);
        }

        var firstServerOnLocation = serversOnLocation.OrderBy(x => x.Usage.AvgRamPercentage).First();

        return new(firstServerOnLocation.Id, null);
    }
}