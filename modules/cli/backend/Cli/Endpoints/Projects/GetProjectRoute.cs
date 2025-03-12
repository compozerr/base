using Api.Abstractions;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects;

public static class GetProjectRoute
{
    public const string Route = "{id:guid}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<ProjectDto?> ExecuteAsync(
        Guid id,
        IProjectRepository projectRepository)
    {
        var projectId = ProjectId.Create(id);

        var project = await projectRepository.GetByIdAsync(projectId);

        if (project is null) return null;

        return ProjectDto.FromProject(project);
    }
}
