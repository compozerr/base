using Auth.Abstractions;
using Auth.Services;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Data.Repositories;

public interface IProjectRepository : IGenericRepository<Project, ProjectId, ApiDbContext>
{
    public Task<List<Project>> GetProjectsForUserAsync(UserId userId);
    public Task<List<Project>> GetProjectsForUserAsync();
    public Task<Project?> GetProjectByIdWithDomainsAsync(ProjectId projectId);
    public Task SetProjectStateAsync(ProjectId projectId, ProjectState state);
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
}