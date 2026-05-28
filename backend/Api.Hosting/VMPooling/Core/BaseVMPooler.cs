using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Auth.Abstractions;
using Cli.Abstractions;
using Database.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Api.Hosting.VMPooling.Core;

// VMPoolItem currently keys on ProjectId, not ProjectEnvironmentId, so we
// resolve via "any server hosting any env of the project" (in practice the
// pool's production env). VMPoolItem schema → env-id is a tracked follow-up.
public abstract class BaseVMPooler(
    VMPool vMPool,
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository,
    IHostingApiFactory hostingApiFactory,
    IEnvironmentTargetResolver environmentTargetResolver,
    IServiceProvider serviceProvider)
{
    internal readonly ServerTierId ServerTierId = ServerTiers.GetById(new ServerTierId(vMPool.ServerTierId)).Id;
    internal readonly LocationId locationId = vMPool.LocationId;
    internal readonly VMPoolId VMPoolId = vMPool.Id;
    internal abstract ProjectType ProjectType { get; }
    internal abstract Uri DefaultRepoUri { get; }
    internal abstract string DefaultProjectName { get; }

    internal virtual void QueueDomainEventsForNewInstance(Project project)
    {
        project.QueueDomainEvent<ProjectCreatedEvent>();
    }

    public virtual async Task CreateNewInstanceAsync()
    {
        var project = new Project
        {
            Name = "N8n pooled project",
            RepoUri = DefaultRepoUri,
            UserId = UserId.Empty,
            LocationId = locationId,
            ServerTierId = ServerTierId,
            State = ProjectState.Stopped,
            Type = ProjectType
        };

        QueueDomainEventsForNewInstance(project);

        await projectRepository.AddAsync(project);

        await vMPoolItemRepository.AddAsync(new VMPoolItem
        {
            ProjectId = project.Id,
            VMPoolId = VMPoolId
        });
    }

    public virtual async Task MakeSureInstanceIsAliveAsync(VMPoolItem item, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(item.ProjectId);
        if (project is null)
        {
            Log.ForContext<BaseVMPooler>()
               .Warning("VMPoolItem {VMPoolItemId} references non-existent project {ProjectId}. Deleting orphaned pool item.", item.Id, item.ProjectId);
            item.DeletedAtUtc = DateTime.UtcNow;
            await vMPoolItemRepository.UpdateAsync(item, cancellationToken);
            return;
        }

        // FOLLOW-UP: VMPoolItem.ProjectEnvironmentId is not modelled yet, so we
        // pick the first env's server via the resolver's project fan-out path.
        // Pooled projects today have a single env, so this is operationally
        // correct; if/when pools span envs, route through env id directly.
        var servers = await environmentTargetResolver.GetDistinctServersForProjectAsync(project.Id, cancellationToken);
        if (servers.Count == 0)
            throw new ServerNotFoundException();
        var hostingApi = await hostingApiFactory.GetHostingApiAsync(servers[0].Id);

        var response = await hostingApi.HealthCheckProjectAsync(project.Id);

        var sender = serviceProvider.GetRequiredService<ISender>();

        switch (response.Code)
        {
            case HealthCheckCode.Unhealthy:
            case HealthCheckCode.NotFound:
                Log.ForContext<BaseVMPooler>()
                   .Information("Project {ProjectId} was not found, redeploying latest deployment.", project.Id);

                var redeployCommand = new RedeployLatestDeploymentCommand(
                    item.ProjectId);

                await sender.Send(
                    redeployCommand,
                    cancellationToken);
                break;
            default:

                break;
        }

        Log.ForContext<BaseVMPooler>()
           .Information("Health check for project {ProjectId} returned code {HealthCheckCode}", project.Id, response.Code);

        item.IsHealthy = response.Code == HealthCheckCode.Healthy;
        await vMPoolItemRepository.UpdateAsync(item, cancellationToken);
    }
}