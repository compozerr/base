using Api.Abstractions;
using Api.Data;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Projects.Environments.Create;

public static class CreateEnvironmentRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddCreateEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<Ok<CreateEnvironmentResponse>> ExecuteAsync(
        ProjectId projectId,
        CreateEnvironmentRequest request,
        IMediator mediator)
    {
        var result = await mediator.Send(new CreateEnvironmentCommand(
            projectId,
            request.Name,
            request.Branch,
            request.Type));

        return TypedResults.Ok(result);
    }
}

public sealed record CreateEnvironmentRequest(
    string Name,
    string Branch,
    EnvironmentType Type = EnvironmentType.Preview);
