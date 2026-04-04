using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Deployments;
using Core.Results;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Deployments;

public static class GetEnvironmentDeploymentsRoute
{
    public const string Route = "{environmentId:guid}/deployments";

    public static RouteHandlerBuilder AddGetEnvironmentDeploymentsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<PagedResult<GetDeploymentResponse>> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        IDeploymentRepository deploymentRepository,
        int page = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var query = deploymentRepository.Query()
            .Where(x => x.ProjectId == projectId && x.EnvironmentId == environmentId);

        var totalCount = await query.CountAsync(cancellationToken);

        var deployments = await query
            .Include(x => x.Project!)
                .ThenInclude(x => x.Domains)
            .Include(x => x.Project!)
                .ThenInclude(x => x.Server!)
                    .ThenInclude(x => x.Location)
            .Include(x => x.Environment)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsPageable(page, pageSize)
            .ToListAsync(cancellationToken);

        var currentDeploymentId = await deploymentRepository.GetCurrentDeploymentId(projectId);

        return new PagedResult<GetDeploymentResponse>(
            page,
            pageSize,
            totalCount,
            [..deployments.Select(x => new GetDeploymentResponse(
                x.Id.Value,
                x.Environment?.GetUrl() ?? x.Project?.Domains?.GetPrimary()?.GetValue ?? "unknown",
                x.Status,
                x.Environment?.Name ?? x.Environment?.Type.ToString() ?? "Production",
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
}
