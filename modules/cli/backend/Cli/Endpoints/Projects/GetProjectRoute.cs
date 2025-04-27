using Api.Abstractions;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects;

public static class GetProjectRoute
{
    public const string Route = "{projectId:guid}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<ProjectDto?> ExecuteAsync(
        ProjectId projectId,
        IProjectRepository projectRepository)
    {
        var project = await projectRepository.GetByIdAsync(projectId);

        if (project is null) return null;

        return ProjectDto.FromProject(project);
    }
}
