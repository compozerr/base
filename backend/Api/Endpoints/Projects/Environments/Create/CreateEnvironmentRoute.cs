using Api.Abstractions;
using Api.Data;
using MediatR;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed record CreateEnvironmentRequest(
    string Name,
    string Branch,
    EnvironmentType Type);

public static class CreateEnvironmentRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddCreateEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<CreateEnvironmentResponse> ExecuteAsync(
        ProjectId projectId,
        CreateEnvironmentRequest request,
        IMediator mediator)
        => mediator.Send(
            new CreateEnvironmentCommand(
                projectId,
                request.Name,
                request.Branch,
                request.Type));
}
