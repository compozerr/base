using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Project.ExecuteCommand;

public static class ExecuteCommandRoute
{
    public const string Route = "{projectId:guid}/exec";

    public static RouteHandlerBuilder AddExecuteCommandRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<ExecuteCommandResponse> ExecuteAsync(
        ProjectId projectId,
        ExecuteCommandRequest request,
        IMediator mediator)
        => mediator.Send(
            new ExecuteCommandCommand(projectId, request.Command));
}

public sealed record ExecuteCommandRequest(string Command);
