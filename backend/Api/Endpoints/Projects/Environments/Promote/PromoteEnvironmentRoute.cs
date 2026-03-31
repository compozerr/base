using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Environments.Promote;

public static class PromoteEnvironmentRoute
{
    public const string Route = "{environmentId:guid}/promote";

    public static RouteHandlerBuilder AddPromoteEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<PromoteEnvironmentResponse> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        IMediator mediator)
        => mediator.Send(new PromoteEnvironmentCommand(projectId, environmentId));
}
