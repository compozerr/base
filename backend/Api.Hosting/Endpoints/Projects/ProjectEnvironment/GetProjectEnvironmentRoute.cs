using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProjectEnvironment;

public sealed record GetProjectEnvironmentResponse(List<ProjectEnvironmentVariableDto> Variables);

public static class GetProjectEnvironmentRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetProjectEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetProjectEnvironmentResponse> ExecuteAsync(
        Guid projectId,
        string branch,
        IProjectRepository projectRepository,
        IDefaultEnvironmentVariablesAppender variablesAppender)
    {
        var projectIdConverted = ProjectId.Create(projectId);

        var environment = await projectRepository.GetProjectEnvironmentByBranchAsync(
            projectIdConverted,
            branch);

        var environmentVariables = environment?.ProjectEnvironmentVariables?.Select(
            x => new ProjectEnvironmentVariableDto(
                x.SystemType,
                x.Key,
                x.Value)).ToList() ?? [];

        environmentVariables = await variablesAppender.AppendDefaultVariablesAsync(
            environmentVariables,
            projectIdConverted);

        return new(environmentVariables);
    }
}
