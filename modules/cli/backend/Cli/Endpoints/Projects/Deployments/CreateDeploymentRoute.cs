using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects.Deployments;

public sealed record CreateDeploymentRequest(string CommitHash);

public static class CreateDeploymentRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddCreateDeploymentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<DeployProjectResponse> ExecuteAsync(Guid projectId, CreateDeploymentRequest request, IMediator mediator)
        => mediator.Send(
            new DeployProjectCommand(
                ProjectId.Create(projectId),
                request.CommitHash));
}
