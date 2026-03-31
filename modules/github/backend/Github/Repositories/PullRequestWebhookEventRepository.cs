using Api.Abstractions;
using Api.Data.Repositories;
using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Github.Repositories;

public interface IPullRequestWebhookEventRepository : IGenericRepository<PullRequestWebhookEvent, PullRequestWebhookEventId, GithubDbContext>
{
    Task<ProjectId?> GetProjectIdFromGitUrlAsync(Uri gitUrl);
}

public sealed class PullRequestWebhookEventRepository(
    GithubDbContext context,
    IServiceScopeFactory serviceScopeFactory) : GenericRepository<PullRequestWebhookEvent, PullRequestWebhookEventId, GithubDbContext>(context), IPullRequestWebhookEventRepository
{
    public async Task<ProjectId?> GetProjectIdFromGitUrlAsync(Uri gitUrl)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

        var urlStr = gitUrl.ToString().TrimEnd('/');
        var withoutGit = urlStr.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? new Uri(urlStr[..^4])
            : gitUrl;
        var withGit = urlStr.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? gitUrl
            : new Uri(urlStr + ".git");

        var projects = await projectRepository.GetFilteredAsync(x => x.RepoUri == withoutGit || x.RepoUri == withGit);
        if (projects.Count > 1)
        {
            throw new InvalidOperationException($"Multiple projects found for git URL: {gitUrl}");
        }

        return projects.Count == 0 ? null : projects[0].Id;
    }
}
