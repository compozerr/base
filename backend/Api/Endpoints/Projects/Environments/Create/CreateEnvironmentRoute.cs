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
            request.Tier,
            request.Type,
            request.ServerId is { } guid ? new ServerId(guid) : null));

        return TypedResults.Ok(result);
    }
}

public sealed record CreateEnvironmentRequest(
    string Name,
    string? Branch,
    string Tier,
    EnvironmentType Type = EnvironmentType.Preview,
    // Plain Guid? on the wire (instead of strongly-typed ServerId?) so the
    // generated OpenAPI/TS schema picks up the nullability — the strongly-
    // typed nullable struct emits as non-nullable string in the qraft config.
    Guid? ServerId = null);
