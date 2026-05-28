using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

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
        // Eager-load envs + Server so the CLI wire payload carries per-env
        // server identity needed for routing (`compozerr ssh -e <env>`).
        // RepoUri is a Uri column; EF can't translate `.ToString() == ...`, so
        // keep the in-memory match shape used previously.
        var userId = currentUserAccessor.CurrentUserId!;
        var userProjects = await projectRepository.Query()
            .Include(p => p.ProjectEnvironments!).ThenInclude(e => e.Server)
            .Where(p => p.UserId == userId)
            .ToListAsync();

        var project = userProjects.FirstOrDefault(p => p.RepoUri.ToString() == Request.RepoUrl);
        if (project is null) return null;

        var prodEnvServerId = project.ProjectEnvironments?
            .FirstOrDefault(e => e.Type == EnvironmentType.Production)?
            .ServerId;

        return ProjectDto.FromProject(project, prodEnvServerId);
    }
}
