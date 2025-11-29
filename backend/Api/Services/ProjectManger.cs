using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Features.N8n.Events;
using Api.Hosting.VMPooling.Core;
using Auth.Abstractions;
using Cli.Abstractions;
using Database.Extensions;

namespace Api.Services;

public interface IProjectManager
{
    Task<ProjectId> AllocateProjectAsync(
        UserId userId,
        string name,
        Uri repoUri,
        ServerTier tier,
        Location location,
        ProjectType projectType,
        CancellationToken cancellationToken);
}

public sealed class ProjectCreator(
    IVMPoolItemDelegator vMPoolItemDelegator,
    IProjectRepository projectRepository,
    ILocationRepository locationRepository
) : IProjectManager
{
    public async Task<ProjectId> AllocateProjectAsync(
        UserId userId,
        string name,
        Uri repoUri,
        ServerTier tier,
        Location location,
        ProjectType projectType,
        CancellationToken cancellationToken)
    {
        
        var newProject = BuildProject(
            userId,
            name,
            repoUri,
            tier,
            location,
            projectType);

        if (projectType == ProjectType.N8n)
        {
            newProject.QueueDomainEvent<N8nProjectCreatedEvent>();
        }


        var project = await projectRepository.AddAsync(
            newProject,
            cancellationToken);

        return project.Id;

    }

    private Task<ProjectId> DelegateExistingProjectAsync(
        string name,
        Uri repoUri,
        string tier,
        string locationIso,
        ProjectType projectType)
    {
        throw new NotImplementedException();
    }

    private static Project BuildProject(
        UserId userId,
        string name,
        Uri repoUri,
        ServerTier serverTier,
        Location location,
        ProjectType projectType)
    {
        var newProject = new Project
        {
            Name = name,
            RepoUri = repoUri,
            UserId = userId,
            LocationId = location.Id,
            ServerTierId = serverTier.Id,
            State = ProjectState.Stopped,
            Type = projectType
        };

        newProject.QueueDomainEvent<ProjectCreatedEvent>();

        return newProject;
    }
}