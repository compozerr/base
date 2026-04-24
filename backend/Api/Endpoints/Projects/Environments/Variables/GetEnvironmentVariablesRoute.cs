using Api.Abstractions;
using Api.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints.Projects.Environments.Variables;

public static class GetEnvironmentVariablesRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetEnvironmentVariablesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetProjectEnvironmentResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        IMediator mediator)
    {
        var response = await mediator.Send(new GetEnvironmentVariablesCommand(projectId, environmentId));
        return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
    }
}
