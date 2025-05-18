using Api.Abstractions;
using Api.Data.Repositories;
using Core.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

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
        var project = await projectRepository.GetByIdAsync(
            projectId,
            cancellationToken,
            getDeleted: true);

        if (project is null) return TypedResults.NotFound();

        if (project.IsDeleted)
            return new Deleted();

        return TypedResults.Ok(ProjectDto.FromProject(project));
    }
}
