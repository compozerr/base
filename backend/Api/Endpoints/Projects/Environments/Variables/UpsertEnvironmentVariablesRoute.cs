using Api.Abstractions;
using Api.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints.Projects.Environments.Variables;

public sealed record UpsertEnvironmentVariablesRequest(
    List<ProjectEnvironmentVariableDto> Variables
);

public static class UpsertEnvironmentVariablesRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddUpsertEnvironmentVariablesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        UpsertEnvironmentVariablesRequest request,
        IMediator mediator)
        => mediator.Send(
            new UpsertEnvironmentVariablesCommand(
                projectId,
                environmentId,
                request.Variables));
}
