using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Auth.Services;
using Core.Results;
using Database.Extensions;
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

    public static async Task<PagedResult<GetDeploymentResponse>> ExecuteAsync(
        ProjectId projectId,
        ICurrentUserAccessor currentUserAccessor,
        IDeploymentRepository deploymentRepository,
        int deploymentStatus = (int)DeploymentStatusFilter.All,
        int page = 1,
        int pageSize = 20)
    {

        var statusFilter = Enum.Parse<DeploymentStatusFilter>(deploymentStatus.ToString());
        var (userDeployments, totalCount) = await GetUserDeploymentsPagedAsync(
            deploymentRepository,
            projectId,
            currentUserAccessor,
            statusFilter,
            page,
            pageSize);

        var currentDeploymentId = await deploymentRepository.GetCurrentDeploymentId();

        return new PagedResult<GetDeploymentResponse>(
            page,
            pageSize,
            totalCount,
            [..userDeployments.Select(x => new GetDeploymentResponse(
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
            ))]
        );
    }

    private static async Task<(List<Deployment>, int TotalCount)> GetUserDeploymentsPagedAsync(
        IDeploymentRepository deploymentRepository,
        ProjectId projectId,
        ICurrentUserAccessor currentUserAccessor,
        DeploymentStatusFilter statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = deploymentRepository.Query()
            .Where(x => x.UserId == currentUserAccessor.CurrentUserId
                && x.ProjectId == projectId);

        if (statusFilter != DeploymentStatusFilter.All)
        {
            var allowedStatuses = statusFilter.ToStatuses();
            query = query.Where(x => allowedStatuses.Contains(x.Status));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var result = await query
            .Include(x => x.Project!)
                .ThenInclude(x => x.Domains)
            .Include(x => x.Project!)
                .ThenInclude(x => x.Server!)
                    .ThenInclude(x => x.Location)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsPageable(page, pageSize)
            .ToListAsync(cancellationToken);

        return (result, totalCount);
    }
}