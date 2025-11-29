using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Abstractions;
using Database.Extensions;

namespace Api.Hosting.VMPooling.Core;

public interface IVMPoolItemDelegator
{
    Task<ProjectId> DelegateAndUpdateAsync(
        VMPoolItemId vmPoolItemId,
        UserId userId,
        string name,
        Uri? newRepoUri = null);
}

public class VMPoolItemDelegator(
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository
) : IVMPoolItemDelegator
{
    public async Task<ProjectId> DelegateAndUpdateAsync(
        VMPoolItemId vmPoolItemId,
        UserId userId,
        string name,
        Uri? newRepoUri = null)
    {
        var vMPoolItem = await vMPoolItemRepository.GetByIdAsync(
            vmPoolItemId) ?? throw new InvalidOperationException($"VMPoolItem with ID {vmPoolItemId} not found.");

        var project = await projectRepository.GetByIdAsync(vMPoolItem.ProjectId) ?? throw new InvalidOperationException($"Project with ID {vMPoolItem.ProjectId} not found.");

        if (project.UserId != UserId.Empty || vMPoolItem.DelegatedAt != null)
        {
            throw new InvalidOperationException($"Project with ID {project.Id} is already assigned to a user.");
        }

        project.UserId = userId;
        project.Name = name;
        project.RepoUri = newRepoUri ?? project.RepoUri;

        project.QueueDomainEvent<ProjectDelegatedEvent>();

        await projectRepository.UpdateAsync(project);

        await vMPoolItemRepository.MarkAsDelegatedAsync(
            vMPoolItem,
            userId,
            DateTime.UtcNow);
            
        return project.Id;
    }
}
