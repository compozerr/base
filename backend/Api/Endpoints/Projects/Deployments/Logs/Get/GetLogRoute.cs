using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.Logs.Get;

public static class GetLogRoute
{
    public const string Route = "{deploymentId:guid}/logs";

    public static RouteHandlerBuilder AddGetLogRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<string> ExecuteAsync(
        ProjectId projectId,
        DeploymentId deploymentId,
        IMediator mediator)
        => mediator.Send(
            new GetLogCommand(
                projectId,
                deploymentId));
}
