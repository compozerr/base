using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Features.N8n.Events;
using Api.Hosting.VMPooling.Core;
using Auth.Abstractions;
using Cli.Abstractions;
using Database.Extensions;
using MediatR;

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

public sealed class ProjectManager(
    IVMPoolItemDelegator vMPoolItemDelegator,
    IProjectRepository projectRepository,
    ISender sender
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
        var vmPoolItemLookupResult = await sender.Send(new VMPoolItemLookupRequest(
            location.Id,
            tier.Id,
            projectType), cancellationToken);

        if (vmPoolItemLookupResult.Found)
        {
            var projectId = await vMPoolItemDelegator.DelegateAndUpdateAsync(
                vmPoolItemLookupResult.VMPoolItemId!,
                userId,
                name,
                repoUri);

            return projectId;
        }

        var newProject = BuildProject(
            userId,
            name,
            repoUri,
            tier,
            location,
            projectType);

        newProject.QueueDomainEvent<ProjectCreatedEvent>();

        if (projectType == ProjectType.N8n)
        {
            newProject.QueueDomainEvent<N8nProjectCreatedEvent>();
        }

        var project = await projectRepository.AddAsync(
            newProject,
            cancellationToken);

        return project.Id;
    }

    private static Project BuildProject(
        UserId userId,
        string name,
        Uri repoUri,
        ServerTier serverTier,
        Location location,
        ProjectType projectType)
        => new()
        {
            Name = name,
            RepoUri = repoUri,
            UserId = userId,
            LocationId = location.Id,
            ServerTierId = serverTier.Id,
            State = ProjectState.Stopped,
            Type = projectType
        };
}