using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Project.Stop;

public static class StopProjectRoute
{
    public const string Route = "{projectId:guid}/stop";

    public static RouteHandlerBuilder AddStopProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(
        ProjectId projectId,
        IMediator mediator)
        => mediator.Send(
            new StopProjectCommand(projectId));
}
