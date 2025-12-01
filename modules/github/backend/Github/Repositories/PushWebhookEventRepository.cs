using Api.Abstractions;
using Api.Data.Repositories;
using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Github.Repositories;

public interface IPushWebhookEventRepository : IGenericRepository<PushWebhookEvent, PushWebhookEventId, GithubDbContext>
{
    Task<ProjectId?> GetProjectIdFromGitUrlAsync(Uri gitUrl);
}

public sealed class PushWebhookEventRepository(
    GithubDbContext context,
    IServiceScopeFactory serviceScopeFactory) : GenericRepository<PushWebhookEvent, PushWebhookEventId, GithubDbContext>(context), IPushWebhookEventRepository
{
    public async Task<ProjectId?> GetProjectIdFromGitUrlAsync(Uri gitUrl)
    {
        // Create a new scope to properly isolate the ApiDbContext from GithubDbContext
        // This prevents memory leaks when used in long-running background jobs
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

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
