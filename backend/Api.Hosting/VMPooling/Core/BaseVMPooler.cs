using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Abstractions;
using Cli.Abstractions;
using Database.Extensions;

namespace Api.Hosting.VMPooling.Core;

public abstract class BaseVMPooler(
    VMPool vMPool,
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository)
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
}