using Api.Data.Repositories;
using Github.Services;
using Github.Utils;
using Jobs;
using Microsoft.EntityFrameworkCore;
using Octokit;

namespace Api.Jobs;

/// <summary>
/// Periodically checks each project's linked GitHub repository and, if the
/// repo has been renamed or transferred on GitHub, updates the stored
/// <see cref="Api.Data.Project.RepoUri"/> to the new canonical URL so deploys
/// keep working without any user action.
/// </summary>
public class RepositoryCheckJob(
    IProjectRepository projectRepository,
    IGithubService githubService) : JobBase<RepositoryCheckJob>
{
    public override async Task ExecuteAsync()
    {
        var projects = await projectRepository.Query().ToListAsync();

        Log.Information("RepositoryCheckJob checking {Count} projects", projects.Count);

        foreach (var project in projects)
        {
            try
            {
                await CheckProjectRepositoryAsync(project);
            }
            catch (Exception ex)
            {
                Log.ForContext("projectId", project.Id)
                   .ForContext("repoUri", project.RepoUri.ToString())
                   .Warning(ex, "Error checking repository for project {ProjectId}", project.Id);
            }
        }
    }

    private async Task CheckProjectRepositoryAsync(Api.Data.Project project)
    {
        if (!GitHubRepoUrl.TryParse(project.RepoUri, out var owner, out var repoName))
        {
            Log.ForContext("projectId", project.Id)
               .ForContext("repoUri", project.RepoUri.ToString())
               .Warning("Could not parse repo URL for project {ProjectId}", project.Id);
            return;
        }

        var storedFullName = $"{owner}/{repoName}";

        var client = await GetClientForProjectAsync(project);
        if (client is null)
        {
            Log.ForContext("projectId", project.Id)
               .Debug("No GitHub client available for project {ProjectId}, skipping", project.Id);
            return;
        }

        Repository repo;
        try
        {
            repo = await client.Repository.Get(owner, repoName);
        }
        catch (NotFoundException)
        {
            Log.ForContext("projectId", project.Id)
               .ForContext("repoUri", project.RepoUri.ToString())
               .Warning("Repository {StoredFullName} not found for project {ProjectId}", storedFullName, project.Id);
            return;
        }

        var currentFullName = repo.FullName;

        if (string.Equals(currentFullName, storedFullName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var newRepoUri = new Uri($"https://github.com/{currentFullName}");
        var oldRepoUri = project.RepoUri;

        project.RepoUri = newRepoUri;
        await projectRepository.UpdateAsync(project);

        Log.ForContext("projectId", project.Id)
           .ForContext("oldRepoUri", oldRepoUri.ToString())
           .ForContext("newRepoUri", newRepoUri.ToString())
           .Information(
               "Project {ProjectId} repository URL auto-updated from {OldFullName} to {NewFullName} (renamed or transferred on GitHub)",
               project.Id,
               storedFullName,
               currentFullName);
    }

    private async Task<IGitHubClient?> GetClientForProjectAsync(Api.Data.Project project)
    {
        // Prefer installation token — more reliable for automated background checks
        // and doesn't depend on the owner's OAuth token still being valid.
        if (project.GithubInstallationId is { } installationId)
        {
            try
            {
                var installationResponse = await githubService
                    .GetInstallationClientByInstallationIdAsync(installationId.ToString());

                if (installationResponse is not null)
                    return installationResponse.InstallationClient;
            }
            catch (Exception ex)
            {
                Log.ForContext("projectId", project.Id)
                   .ForContext("installationId", installationId)
                   .Debug(ex, "Failed to get installation client, falling back to user client");
            }
        }

        // Fall back to the project owner's user client.
        return await githubService.GetUserClient(project.UserId);
    }
}
