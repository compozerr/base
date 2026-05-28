using Api.Abstractions;
using Api.Data.Repositories;
using Core.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Cli.Endpoints.Projects;

public static class GetProjectRoute
{
    public const string Route = "{projectId:guid}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<ProjectDto>, NotFound, Deleted>> ExecuteAsync(
        ProjectId projectId,
        IProjectRepository projectRepository,
        CancellationToken cancellationToken = default)
    {
        // Eager-load envs + their Server so the CLI wire payload carries the
        // per-env server identity needed for `compozerr ssh -e <env>` etc.
        var project = await projectRepository.Query(getDeleted: true)
            .Include(p => p.ProjectEnvironments!).ThenInclude(e => e.Server)
            .SingleOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null) return TypedResults.NotFound();

        if (project.IsDeleted)
            return new Deleted();

        var prodEnvServerId = project.ProjectEnvironments?
            .FirstOrDefault(e => e.Type == Api.Data.EnvironmentType.Production)?
            .ServerId;

        return TypedResults.Ok(ProjectDto.FromProject(project, prodEnvServerId));
    }
}
