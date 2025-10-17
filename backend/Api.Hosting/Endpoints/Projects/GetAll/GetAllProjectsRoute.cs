using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.GetAll;

public sealed record ProjectDto(Guid ProjectId);

public static class GetAllProjectsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetAllProjectsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Ok<List<ProjectDto>>> ExecuteAsync(
        IProjectRepository projectRepository)
    {
        var projects = await projectRepository.GetAllAsync();

        var projectDtos = projects
            .Select(p => new ProjectDto(p.Id.Value))
            .ToList();

        return TypedResults.Ok(projectDtos);
    }
}
