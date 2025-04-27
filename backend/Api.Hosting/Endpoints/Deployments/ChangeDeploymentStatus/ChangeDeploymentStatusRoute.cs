using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;

public sealed record ChangeDeploymentStatusRequest(string Status);

public static class ChangeDeploymentStatusRoute
{
    public const string Route = "{deploymentId:guid}/status";

    public static RouteHandlerBuilder AddChangeDeploymentStatusRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static Task<ChangeDeploymentStatusResponse> ExecuteAsync(
        DeploymentId deploymentId,
        ChangeDeploymentStatusRequest request,
        IMediator mediator)
            => mediator.Send(
                new ChangeDeploymentStatusCommand(
                    deploymentId,
                    Enum.TryParse<DeploymentStatus>(request.Status, true, out var status)
                        ? status
                        : throw new ArgumentException("Invalid deployment status")));
}
