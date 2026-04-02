using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IEnvironmentRepository : IGenericRepository<Environment, EnvironmentId, ApiDbContext>
{
    Task<List<Environment>> GetByProjectIdAsync(ProjectId projectId, CancellationToken cancellationToken = default);
    Task<Environment?> GetByIdWithProjectAsync(EnvironmentId environmentId, CancellationToken cancellationToken = default);
}

public sealed class EnvironmentRepository(
    ApiDbContext context) : GenericRepository<Environment, EnvironmentId, ApiDbContext>(context), IEnvironmentRepository
{
    public Task<List<Environment>> GetByProjectIdAsync(ProjectId projectId, CancellationToken cancellationToken = default)
        => Query()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<Environment?> GetByIdWithProjectAsync(EnvironmentId environmentId, CancellationToken cancellationToken = default)
        => Query()
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == environmentId, cancellationToken);
}
