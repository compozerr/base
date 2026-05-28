using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IProjectEnvironmentRepository : IGenericRepository<ProjectEnvironment, ProjectEnvironmentId, ApiDbContext>
{
    public Task<ProjectEnvironment?> GetProjectEnvironmentByBranchAsync(ProjectId projectId, string branch);
    public Task<ProjectId> UpsertProjectEnvironmentAsync(ProjectId projectId, string branch, ProjectEnvironmentVariableDto[] pairs);
    public Task<List<ProjectEnvironment>> GetByProjectIdWithLatestDeploymentAsync(ProjectId projectId, CancellationToken cancellationToken = default);
    public Task<List<ProjectEnvironment>> GetByProjectIdAsync(ProjectId projectId, CancellationToken cancellationToken = default);
    public Task<ProjectEnvironment?> GetProductionEnvironmentAsync(ProjectId projectId);
    public Task<ProjectEnvironment?> GetProjectEnvironmentByNameAsync(ProjectId projectId, string name);
    public Task<ProjectEnvironment?> GetByIdWithVariablesAsync(ProjectEnvironmentId environmentId);
    Task<List<ProjectEnvironment>> GetAllActiveWithProjectAndServerAsync(CancellationToken cancellationToken = default);
}

public sealed class ProjectEnvironmentRepository(
    ApiDbContext context) : GenericRepository<ProjectEnvironment, ProjectEnvironmentId, ApiDbContext>(context), IProjectEnvironmentRepository
{
    /// <summary>
    /// Returns the environment whose <see cref="ProjectEnvironment.Branches"/> contains <paramref name="branch"/>
    /// for the given <paramref name="projectId"/>. When multiple environments share the branch (e.g. a Production
    /// env and a Preview/staging env both auto-deploying from <c>main</c>), the result is deterministic:
    /// Production wins (lowest <see cref="ProjectEnvironment.Type"/> value), then most recently created.
    /// Returns <c>null</c> when no environment matches.
    /// </summary>
    public Task<ProjectEnvironment?> GetProjectEnvironmentByBranchAsync(ProjectId projectId, string branch)
        => Query()
                .Include(x => x.ProjectEnvironmentVariables)
                .Where(x => x.ProjectId == projectId && x.Branches.Contains(branch))
                .OrderBy(x => x.Type)
                .ThenByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();

    public Task<List<ProjectEnvironment>> GetByProjectIdWithLatestDeploymentAsync(ProjectId projectId, CancellationToken cancellationToken = default)
        => Query()
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.Deployments!.OrderByDescending(d => d.CreatedAtUtc).Take(1))
            .Include(e => e.Project!).ThenInclude(p => p.Domains)
            .Include(e => e.Project!).ThenInclude(p => p.Server!).ThenInclude(s => s.Location)
            .OrderBy(e => e.Type)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<List<ProjectEnvironment>> GetByProjectIdAsync(ProjectId projectId, CancellationToken cancellationToken = default)
        => Query()
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Type)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<ProjectEnvironment?> GetProductionEnvironmentAsync(ProjectId projectId)
        => Query()
            .Include(x => x.ProjectEnvironmentVariables)
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Type == EnvironmentType.Production);

    public Task<ProjectEnvironment?> GetProjectEnvironmentByNameAsync(ProjectId projectId, string name)
        => Query()
            .Include(x => x.ProjectEnvironmentVariables)
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Name == name);

    public Task<ProjectEnvironment?> GetByIdWithVariablesAsync(ProjectEnvironmentId environmentId)
        => Query()
            .Include(x => x.ProjectEnvironmentVariables)
            .SingleOrDefaultAsync(x => x.Id == environmentId);

    public Task<List<ProjectEnvironment>> GetAllActiveWithProjectAndServerAsync(CancellationToken cancellationToken = default)
        => Query()
            .Include(e => e.Project)
            .Where(e => e.Project != null && e.Project.DeletedAtUtc == null && e.Project.ServerId != null)
            .Where(e => e.BillingStatus == EnvironmentBillingStatus.Active)
            .ToListAsync(cancellationToken);

    public async Task<ProjectId> UpsertProjectEnvironmentAsync(ProjectId projectId, string branch, ProjectEnvironmentVariableDto[] pairs)
    {
        var environment = await GetProjectEnvironmentByBranchAsync(projectId, branch);

        environment ??= new ProjectEnvironment
        {
            Branches = [branch],
            ProjectId = projectId,
            AutoDeploy = true
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

        await UpdateAsync(environment);

        return projectId;
    }
};