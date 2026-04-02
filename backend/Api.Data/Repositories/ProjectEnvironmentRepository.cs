using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IProjectEnvironmentRepository : IGenericRepository<ProjectEnvironment, ProjectEnvironmentId, ApiDbContext>
{
    public Task<ProjectEnvironment?> GetProjectEnvironmentByBranchAsync(ProjectId projectId, string branch);
    public Task<ProjectId> UpsertProjectEnvironmentAsync(ProjectId projectId, string branch, ProjectEnvironmentVariableDto[] pairs);
    public Task<List<ProjectEnvironment>> GetByProjectIdWithLatestDeploymentAsync(ProjectId projectId, CancellationToken cancellationToken = default);
}

public sealed class ProjectEnvironmentRepository(
    ApiDbContext context) : GenericRepository<ProjectEnvironment, ProjectEnvironmentId, ApiDbContext>(context), IProjectEnvironmentRepository
{
    public Task<ProjectEnvironment?> GetProjectEnvironmentByBranchAsync(ProjectId projectId, string branch)
        => Query()
                .Include(x => x.ProjectEnvironmentVariables)
                .SingleOrDefaultAsync(x => x.Branches.Contains(branch) && x.ProjectId == projectId);

    public Task<List<ProjectEnvironment>> GetByProjectIdWithLatestDeploymentAsync(ProjectId projectId, CancellationToken cancellationToken = default)
        => Query()
            .Where(e => e.ProjectId == projectId)
            .Include(e => e.Deployments!.OrderByDescending(d => d.CreatedAtUtc).Take(1))
            .OrderBy(e => e.Type)
            .ThenByDescending(e => e.CreatedAtUtc)
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