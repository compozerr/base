using Api.Abstractions;
using Api.Data;
using MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public sealed record UpsertProjectEnvironmentVariablesRequest(
    List<ProjectEnvironmentVariableDto> Variables
);

public static class UpsertProjectEnvironmentVariablesRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddUpsertProjectEnvironmentVariablesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(
        ProjectId projectId,
        string branch,
        UpsertProjectEnvironmentVariablesRequest request,
        IMediator mediator)
        => mediator.Send(
            new UpsertProjectEnvironmentVariablesCommand(
                projectId,
                branch,
                request.Variables));
}
