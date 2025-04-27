using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Project.Delete;

public static class DeleteProjectRoute
{
    public const string Route = "{projectId:guid}";

    public static RouteHandlerBuilder AddDeleteProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, ExecuteAsync);
    }

    public static Task<DeleteProjectResponse> ExecuteAsync(
        ProjectId projectId,
        IMediator mediator)
        => mediator.Send(
            new DeleteProjectCommand(projectId));
}
