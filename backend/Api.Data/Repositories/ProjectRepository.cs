using Auth.Abstractions;
using Auth.Services;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Serilog;
using Api.Data.Extensions;
using Database.Extensions;

namespace Api.Data.Repositories;

public interface IProjectRepository : IGenericRepository<Project, ProjectId, ApiDbContext>
{
    public Task<List<Project>> GetProjectsForUserAsync(UserId userId);
    public Task<List<Project>> GetProjectsForUserAsync();
    public Task<List<Project>> GetProjectsForOrganizationAsync(OrganizationId orgId);
    public Task<Project?> GetProjectByIdWithDomainsAsync(ProjectId projectId);
    public Task SetProjectStateAsync(ProjectId projectId, ProjectState state);
    public Task<(List<Project> Projects, int TotalCount, int RunningProjectsCount, Dictionary<ProjectId, DeploymentId> ActiveDeployments)> GetProjectsForUserPagedAsync(int page, int pageSize, string? search, ProjectStateFilter stateFilter);
    public Task<(List<Project> Projects, int TotalCount, int RunningProjectsCount, Dictionary<ProjectId, DeploymentId> ActiveDeployments)> GetProjectsForOrganizationPagedAsync(OrganizationId orgId, int page, int pageSize, string? search, ProjectStateFilter stateFilter);
}

public sealed class ProjectRepository(
    ApiDbContext context,
    ICurrentUserAccessor currentUserAccessor) : GenericRepository<Project, ProjectId, ApiDbContext>(context), IProjectRepository
{
    public Task<Project?> GetProjectByIdWithDomainsAsync(ProjectId projectId)
        => Query()
            // Filter soft-deleted domains out of the Include. Otherwise this method
            // pulls every Domain row for the project (including tombstoned ones) into
            // the scoped DbContext's change tracker. Any other handler that runs in
            // the same scope and touches project.Domains via a filtered Include then
            // sees those tombstones show up in its navigation collection via EF Core
            // relationship fix-up. That's how the service-reporting pipeline was
            // crashing on soft-deleted cross-env domains even though the outer query
            // had its own filter. See docs/plans/2026-04-08-services-reporting-bugs-handoff.md.
            .Include(x => x.Domains!.Where(d => d.DeletedAtUtc == null))
            .Include(x => x.Server)
            .SingleOrDefaultAsync(x => x.Id == projectId);


    public Task<List<Project>> GetProjectsForUserAsync(UserId userId)
        => Query()
            .Include(x => x.Domains)
            .Where(x => x.UserId == userId)
            .ToListAsync();

    public Task<List<Project>> GetProjectsForUserAsync()
    {
        var userId = currentUserAccessor.CurrentUserId!;

        return GetProjectsForUserAsync(userId);
    }

    public Task<List<Project>> GetProjectsForOrganizationAsync(OrganizationId orgId)
        => Query()
            .Include(x => x.Domains)
            .Where(x => x.OrganizationId == orgId)
            .ToListAsync();

    public async Task SetProjectStateAsync(ProjectId projectId, ProjectState state)
    {
        var project = await GetByIdAsync(projectId);

        if (project is null)
        {
            Log.ForContext("projectId", projectId)
                .ForContext("method", nameof(SetProjectStateAsync))
                .Error("Project not found");

            return;
        }

        project.State = state;

        await UpdateAsync(project);
    }

    public async Task<(List<Project> Projects, int TotalCount, int RunningProjectsCount, Dictionary<ProjectId, DeploymentId> ActiveDeployments)> GetProjectsForUserPagedAsync(int page, int pageSize, string? search, ProjectStateFilter stateFilter)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        var query = Query()
            .Include(x => x.Domains)
            .Where(x => x.UserId == userId);

        // Filter by state
        if (stateFilter != ProjectStateFilter.All)
        {
            var allowedStates = stateFilter.ToStates();
            query = query.Where(x => allowedStates.Contains(x.State));
        }

        // Filter by search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var runningProjectsCount = await query.CountAsync(x => x.State == ProjectState.Running);

        // Sort by latest deployment activity, falling back to project update/create date
        var projects = await query.OrderByDescending(x =>
                context.Deployments
                    .Where(d => d.ProjectId == x.Id)
                    .Max(d => (DateTime?)d.CreatedAtUtc)
                ?? x.UpdatedAtUtc
                ?? x.CreatedAtUtc)
            .AsPageable(page, pageSize)
            .ToListAsync();

        // Get active (Deploying) deployments for the returned projects
        var projectIds = projects.Select(p => p.Id).ToList();
        var activeDeployments = await context.Deployments
            .Where(d => projectIds.Contains(d.ProjectId)
                     && (d.Status == DeploymentStatus.Deploying || d.Status == DeploymentStatus.Queued))
            .GroupBy(d => d.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                DeploymentId = g.OrderByDescending(d => d.CreatedAtUtc).First().Id
            })
            .ToDictionaryAsync(x => x.ProjectId, x => x.DeploymentId);

        return (projects, totalCount, runningProjectsCount, activeDeployments);
    }

    public async Task<(List<Project> Projects, int TotalCount, int RunningProjectsCount, Dictionary<ProjectId, DeploymentId> ActiveDeployments)> GetProjectsForOrganizationPagedAsync(OrganizationId orgId, int page, int pageSize, string? search, ProjectStateFilter stateFilter)
    {
        var query = Query()
            .Include(x => x.Domains)
            .Where(x => x.OrganizationId == orgId);

        if (stateFilter != ProjectStateFilter.All)
        {
            var allowedStates = stateFilter.ToStates();
            query = query.Where(x => allowedStates.Contains(x.State));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var runningProjectsCount = await query.CountAsync(x => x.State == ProjectState.Running);

        var projects = await query.OrderByDescending(x =>
                context.Deployments
                    .Where(d => d.ProjectId == x.Id)
                    .Max(d => (DateTime?)d.CreatedAtUtc)
                ?? x.UpdatedAtUtc
                ?? x.CreatedAtUtc)
            .AsPageable(page, pageSize)
            .ToListAsync();

        var projectIds = projects.Select(p => p.Id).ToList();
        var activeDeployments = await context.Deployments
            .Where(d => projectIds.Contains(d.ProjectId)
                     && (d.Status == DeploymentStatus.Deploying || d.Status == DeploymentStatus.Queued))
            .GroupBy(d => d.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                DeploymentId = g.OrderByDescending(d => d.CreatedAtUtc).First().Id
            })
            .ToDictionaryAsync(x => x.ProjectId, x => x.DeploymentId);

        return (projects, totalCount, runningProjectsCount, activeDeployments);
    }
}