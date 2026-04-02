using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Environments.Delete;

public static class DeleteEnvironmentRoute
{
    public const string Route = "{environmentId:guid}";

    public static RouteHandlerBuilder AddDeleteEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, ExecuteAsync);
    }

    public static Task<DeleteEnvironmentResponse> ExecuteAsync(
        ProjectId projectId,
        EnvironmentId environmentId,
        IMediator mediator)
        => mediator.Send(
            new DeleteEnvironmentCommand(
                environmentId));
}
