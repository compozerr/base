using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Auth.Abstractions;
using Cli.Abstractions;
using Database.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Api.Hosting.VMPooling.Core;

public abstract class BaseVMPooler(
    VMPool vMPool,
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository,
    IHostingApiFactory hostingApiFactory,
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
        var project = await projectRepository.GetByIdAsync(item.ProjectId) ?? throw new InvalidOperationException($"Project with ID {item.ProjectId} not found for VMPoolItem {item.Id}");

        var hostingApi = await hostingApiFactory.GetHostingApiAsync(project.ServerId!);

        var response = await hostingApi.HealthCheckProjectAsync(project.Id);

        var sender = serviceProvider.GetRequiredService<ISender>();

        switch (response.Code)
        {
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
    }
}