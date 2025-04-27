using Auth.Abstractions;
using Auth.Services;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Data.Repositories;

public interface IProjectRepository : IGenericRepository<Project, ProjectId, ApiDbContext>
{
    public Task<ProjectId> UpsertProjectEnvironmentAsync(ProjectId projectId, string branch, ProjectEnvironmentVariableDto[] pairs);
    public Task<ProjectEnvironment?> GetProjectEnvironmentByBranchAsync(ProjectId projectId, string branch);
    public Task<List<Project>> GetProjectsForUserAsync(UserId userId);
    public Task<List<Project>> GetProjectsForUserAsync();
    public Task<Project?> GetProjectByIdWithDomainsAsync(ProjectId projectId);
    public Task SetProjectStateAsync(ProjectId projectId, ProjectState state);
}

public sealed class ProjectRepository(
    ApiDbContext context,
    ICurrentUserAccessor currentUserAccessor) : GenericRepository<Project, ProjectId, ApiDbContext>(context), IProjectRepository
{
    private readonly ApiDbContext _context = context;

    public Task<Project?> GetProjectByIdWithDomainsAsync(ProjectId projectId)
        => _context.Projects
            .Include(x => x.Domains)
            .Include(x => x.Server)
            .SingleOrDefaultAsync(x => x.Id == projectId);

    public Task<ProjectEnvironment?> GetProjectEnvironmentByBranchAsync(ProjectId projectId, string branch)
        => _context.ProjectEnvironments
                .Include(x => x.ProjectEnvironmentVariables)
                .SingleOrDefaultAsync(x => x.Branches.Contains(branch) && x.ProjectId == projectId);

    public Task<List<Project>> GetProjectsForUserAsync(UserId userId)
        => _context.Projects
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

    public async Task<ProjectId> UpsertProjectEnvironmentAsync(ProjectId projectId, string branch, ProjectEnvironmentVariableDto[] pairs)
    {
        var environment = await GetProjectEnvironmentByBranchAsync(projectId, branch);

        environment ??= new ProjectEnvironment
        {
            Branches = [branch],
            ProjectId = projectId
        };

        var variables = environment.ProjectEnvironmentVariables ?? [];

        foreach (var pair in pairs)
        {
            var variable = variables.SingleOrDefault(x => x.Key == pair.Key);

            if (variable is null)
            {
                variables.Add(new ProjectEnvironmentVariable
                {
                    SystemType = pair.SystemType,
                    Key = pair.Key,
                    Value = pair.Value,
                });
            }
            else
            {
                variable.Value = pair.Value;
            }
        }

        _context.ProjectEnvironments.Update(environment);

        await _context.SaveChangesAsync();

        return projectId;
    }
}