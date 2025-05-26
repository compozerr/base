using Auth.Abstractions;
using Auth.Services;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Api.Data.Extensions;

namespace Api.Data.Repositories;

public interface IProjectRepository : IGenericRepository<Project, ProjectId, ApiDbContext>
{
    public Task<List<Project>> GetProjectsForUserAsync(UserId userId);
    public Task<List<Project>> GetProjectsForUserAsync();
    public Task<Project?> GetProjectByIdWithDomainsAsync(ProjectId projectId);
    public Task SetProjectStateAsync(ProjectId projectId, ProjectState state);
    public Task<(List<Project> Projects, int TotalCount, int RunningProjectsCount)> GetProjectsForUserPagedAsync(int page, int pageSize, string? search, ProjectStateFilter stateFilter);
}

public sealed class ProjectRepository(
    ApiDbContext context,
    ICurrentUserAccessor currentUserAccessor) : GenericRepository<Project, ProjectId, ApiDbContext>(context), IProjectRepository
{
    public Task<Project?> GetProjectByIdWithDomainsAsync(ProjectId projectId)
        => Query()
            .Include(x => x.Domains)
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

    public async Task<(List<Project> Projects, int TotalCount, int RunningProjectsCount)> GetProjectsForUserPagedAsync(int page, int pageSize, string? search, ProjectStateFilter stateFilter)
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
        var projects = await ApplyPagination(
                query.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc),
                page, pageSize)
            .ToListAsync();

        return (projects, totalCount, runningProjectsCount);
    }
}