using Api.Data.Repositories;
using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects;

public sealed record GetProjectByRepoUrlRequest(string RepoUrl);

public static class GetProjectByRepoUrlRoute
{
    public const string Route = "get-project-by-repo-url";

    public static RouteHandlerBuilder AddGetProjectByRepoUrlRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<ProjectDto?> ExecuteAsync(
        GetProjectByRepoUrlRequest Request,
        ICurrentUserAccessor currentUserAccessor,
        IProjectRepository projectRepository)
    {
        var allUserProjects = await projectRepository.GetProjectsForUserAsync();

        var project = allUserProjects.FirstOrDefault(p => p.RepoUri.ToString() == Request.RepoUrl);

        if (project is null) return null;

        return ProjectDto.FromProject(project);
    }
}
