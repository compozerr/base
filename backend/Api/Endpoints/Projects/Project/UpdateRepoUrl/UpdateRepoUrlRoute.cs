using Api.Abstractions;
using Api.Data.Repositories;
using Auth.Services;
using Github.Services;
using Github.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Organizations.Services;
using Serilog;

namespace Api.Endpoints.Projects.Project.UpdateRepoUrl;

public static class UpdateRepoUrlRoute
{
    public const string Route = "{projectId:guid}/repo-url";

    public static RouteHandlerBuilder AddUpdateRepoUrlRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<UpdateRepoUrlResponse>, NotFound, BadRequest<string>>> ExecuteAsync(
        ProjectId projectId,
        UpdateRepoUrlRequest request,
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

        // Validate the URL is a valid GitHub URL
        if (!GitHubRepoUrl.TryParse(request.RepoUrl, out var newOwner, out var newRepoName))
        {
            return TypedResults.BadRequest("Invalid GitHub repository URL.");
        }

        // Verify the user has access to the new repository
        var hasRepoAccess = await githubService.HasAccessToRepositoryAsync(request.RepoUrl, userId);
        if (!hasRepoAccess)
        {
            return TypedResults.BadRequest("You do not have access to this repository. Ensure the Compozerr GitHub App is installed on the repository's organization or account.");
        }

        var oldRepoUri = project.RepoUri;
        var newRepoUri = new Uri(request.RepoUrl.TrimEnd('/'));

        project.RepoUri = newRepoUri;
        await projectRepository.UpdateAsync(project, cancellationToken);

        Log.ForContext("projectId", projectId)
           .ForContext("oldRepoUri", oldRepoUri.ToString())
           .ForContext("newRepoUri", newRepoUri.ToString())
           .Information("Project repository URL updated");

        return TypedResults.Ok(new UpdateRepoUrlResponse(
            $"{newOwner}/{newRepoName}"));
    }
}
