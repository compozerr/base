using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Auth.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Deployments;

public sealed record GetDeploymentResponse(
    Guid Id,
    string Url,
    DeploymentStatus Status,
    string Environment,
    string Branch,
    string CommitHash,
    string CommitMessage,
    DateTime CreatedAt,
    string Author,
    bool IsCurrent,
    TimeSpan BuildDuration,
    string Region,
    List<string> BuildLogs
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
        var deployments = await deploymentRepository.GetByProjectIdAsync(
            ProjectId.Create(projectId),
            x => x
                .Include(x => x.Project!)
                    .ThenInclude(x => x.Domains)
                .Include(x => x.Project!)
                    .ThenInclude(x => x.Server!)
                        .ThenInclude(x => x.Location));

        var userDeployments = deployments.Where(x => x.UserId == currentUserAccessor.CurrentUserId)
                                         .OrderByDescending(x=>x.CreatedAtUtc)
                                         .ToList();

        var currentDeploymentId = await deploymentRepository.GetCurrentDeploymentId();

        return [..userDeployments.Select(x => new GetDeploymentResponse(
            x.Id.Value,
            x.Project?.Domains?.GetPrimary()?.GetValue ?? "unknown",
            x.Status,
            "Production",
            x.CommitBranch,
            x.CommitHash,
            x.CommitMessage,
            x.CreatedAtUtc,
            x.CommitAuthor,
            x.Id == currentDeploymentId,
            x.GetBuildDuration(),
            x.Project?.Server?.Location?.IsoCountryCode ?? "unknown",
            []
        ))];
    }
}