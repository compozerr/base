using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;

namespace Api.Endpoints.Projects.Deployments;

public sealed record GetDeploymentResponse(
    Guid Id,
    DeploymentStatus Status,
    string Environment,
    string Branch,
    string CommitHash,
    string CommitMessage,
    DateTime CreatedAt,
    string Creator,
    bool IsCurrent
);

public static class GetDeploymentsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetDeploymentsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<List<GetDeploymentResponse>> ExecuteAsync(
        Guid projectId,
        ICurrentUserAccessor currentUserAccessor,
        IDeploymentRepository deploymentRepository)
    {
        var deployments = await deploymentRepository.GetByProjectIdAsync(ProjectId.Create(projectId));

        var userDeployments = deployments.Where(x => x.UserId == currentUserAccessor.CurrentUserId)
                                         .ToList();

        var current = userDeployments.Where(x => x.Status == DeploymentStatus.Completed)
                                     .OrderByDescending(x => x.CreatedAtUtc)
                                     .FirstOrDefault();

        return [..userDeployments.Select(x => new GetDeploymentResponse(
            x.Id.Value,
            x.Status,
            "Production",
            "main",
            x.CommitHash,
            "CommitMessage",
            x.CreatedAtUtc,
            "Creator",
            current != null && current == x
        ))];
    }
}