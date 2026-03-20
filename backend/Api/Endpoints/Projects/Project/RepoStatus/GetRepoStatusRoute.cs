using Api.Abstractions;
using Api.Data.Repositories;
using Auth.Services;
using Github.Services;
using Github.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Octokit;
using Organizations.Services;
using Serilog;

namespace Api.Endpoints.Projects.Project.RepoStatus;

public static class GetRepoStatusRoute
{
    public const string Route = "{projectId:guid}/repo-status";

    public static RouteHandlerBuilder AddGetRepoStatusRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<RepoStatusResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        ICurrentUserAccessor currentUserAccessor,
        IProjectRepository projectRepository,
        IOrganizationAuthorizationService orgAuthService,
        IGithubService githubService,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
            return TypedResults.NotFound();

        var userId = currentUserAccessor.CurrentUserId!;
        var hasAccess = project.OrganizationId is not null
            ? await orgAuthService.CanAccessProjectAsync(project.OrganizationId, userId)
            : project.UserId == userId;

        if (!hasAccess)
            return TypedResults.NotFound();

        var (owner, repoName) = GitHubRepoUrl.Parse(project.RepoUri);
        var storedFullName = $"{owner}/{repoName}";

        try
        {
            var userClient = await githubService.GetUserClient(userId);
            if (userClient is null)
            {
                return TypedResults.Ok(new RepoStatusResponse(
                    Status: RepoStatus.Error,
                    CurrentName: storedFullName,
                    Message: "GitHub account not connected. Please reconnect your account."));
            }

            Repository repo;
            try
            {
                repo = await userClient.Repository.Get(owner, repoName);
            }
            catch (NotFoundException)
            {
                return TypedResults.Ok(new RepoStatusResponse(
                    Status: RepoStatus.NotFound,
                    CurrentName: storedFullName,
                    Message: "Repository not found. It may have been deleted, made private, or you may have lost access."));
            }

            var currentFullName = repo.FullName;
            var isArchived = repo.Archived;
            var isPrivate = repo.Private;
            var defaultBranch = repo.DefaultBranch;

            // Check if repo was renamed or transferred
            if (!string.Equals(currentFullName, storedFullName, StringComparison.OrdinalIgnoreCase))
            {
                var currentOwner = currentFullName.Split('/')[0];
                var wasTransferred = !string.Equals(currentOwner, owner, StringComparison.OrdinalIgnoreCase);

                return TypedResults.Ok(new RepoStatusResponse(
                    Status: wasTransferred ? RepoStatus.Transferred : RepoStatus.Renamed,
                    CurrentName: storedFullName,
                    NewName: currentFullName,
                    NewUrl: $"https://github.com/{currentFullName}",
                    IsPrivate: isPrivate,
                    IsArchived: isArchived,
                    DefaultBranch: defaultBranch));
            }

            if (isArchived)
            {
                return TypedResults.Ok(new RepoStatusResponse(
                    Status: RepoStatus.Archived,
                    CurrentName: storedFullName,
                    IsPrivate: isPrivate,
                    IsArchived: true,
                    DefaultBranch: defaultBranch));
            }

            return TypedResults.Ok(new RepoStatusResponse(
                Status: RepoStatus.Ok,
                CurrentName: storedFullName,
                IsPrivate: isPrivate,
                IsArchived: false,
                DefaultBranch: defaultBranch));
        }
        catch (Exception ex)
        {
            Log.ForContext("projectId", projectId)
               .ForContext("repoUri", project.RepoUri.ToString())
               .Warning(ex, "Failed to check repository status");

            return TypedResults.Ok(new RepoStatusResponse(
                Status: RepoStatus.Error,
                CurrentName: storedFullName,
                Message: "Failed to check repository status. Please try again later."));
        }
    }
}
