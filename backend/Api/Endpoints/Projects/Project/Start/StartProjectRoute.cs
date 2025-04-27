using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Project.Start;

public static class StartProjectRoute
{
    public const string Route = "{projectId:guid}/start";

    public static RouteHandlerBuilder AddStartProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(
        ProjectId projectId,
        IMediator mediator)
        => mediator.Send(
            new StartProjectCommand(projectId));
}
