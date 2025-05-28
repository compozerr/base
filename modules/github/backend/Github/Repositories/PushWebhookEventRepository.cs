using Api.Abstractions;
using Api.Data.Repositories;
using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;

namespace Github.Repositories;

public interface IPushWebhookEventRepository : IGenericRepository<PushWebhookEvent, PushWebhookEventId, GithubDbContext>
{
    Task<ProjectId?> GetProjectIdFromGitUrlAsync(Uri gitUrl);
}

public sealed class PushWebhookEventRepository(
    GithubDbContext context,
    IProjectRepository projectRepository) : GenericRepository<PushWebhookEvent, PushWebhookEventId, GithubDbContext>(context), IPushWebhookEventRepository
{
    public async Task<ProjectId?> GetProjectIdFromGitUrlAsync(Uri gitUrl)
    {
        var projects = await projectRepository.GetFilteredAsync(x => x.RepoUri == gitUrl);
        if (projects.Count > 1)
        {
            throw new InvalidOperationException($"Multiple projects found for git URL: {gitUrl}");
        }

        if (projects.Count == 0)
        {
            return null;
        }

        return projects[0].Id;
    }
}
