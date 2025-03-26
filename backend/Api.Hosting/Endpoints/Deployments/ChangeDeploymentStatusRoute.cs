using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Deployments;

public sealed record ChangeDeploymentStatusRequest(string Status);
public sealed record ChangeDeploymentStatusResponse(bool Success);

public static class ChangeDeploymentStatusRoute
{
    public const string Route = "{deploymentId:guid}/status";

    public static RouteHandlerBuilder AddChangeDeploymentStatusRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static async Task<ChangeDeploymentStatusResponse> ExecuteAsync(
        Guid deploymentId,
        ChangeDeploymentStatusRequest request,
        IDeploymentRepository deploymentRepository)
    {
        var deploymentStatus = Enum.TryParse<DeploymentStatus>(request.Status, true, out var status)
            ? status
            : throw new ArgumentException("Invalid deployment status");

        var deployment = await deploymentRepository.GetByIdAsync(DeploymentId.Create(deploymentId)) ?? throw new ArgumentException("Deployment not found");

        deployment.Status = deploymentStatus;
        deployment.BuildDuration = deployment.GetBuildDuration();

        await deploymentRepository.UpdateAsync(deployment);

        return new(true);
    }
}
