using Api.Abstractions;
using Api.Data;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.EnvironmentVariables;

public static class GetEnvironmentVariablesByIdRoute
{
    public const string Route = "{environmentId:guid}/variables";

    public static RouteHandlerBuilder AddGetEnvironmentVariablesByIdRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetProjectEnvironmentResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        IMediator mediator)
    {
        var response = await mediator.Send(new GetEnvironmentVariablesByIdCommand(projectId, environmentId));
        return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
    }
}
