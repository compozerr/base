using Auth.Abstractions;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IDeploymentRepository : IGenericRepository<Deployment, DeploymentId, ApiDbContext>
{
    public Task<List<Deployment>> GetDeploymentsForUserAsync(UserId userId);
    public Task<List<Deployment>> GetByProjectIdAsync(ProjectId projectId);
    public Task<List<Deployment>> GetByProjectIdAsync(ProjectId projectId, Func<IQueryable<Deployment>, IQueryable<Deployment>> includeBuilder);
}

public sealed class DeploymentRepository(
    ApiDbContext context) : GenericRepository<Deployment, DeploymentId, ApiDbContext>(context), IDeploymentRepository
{
    private readonly ApiDbContext _context = context;

    public Task<List<Deployment>> GetByProjectIdAsync(ProjectId projectId)
        => _context.Deployments.Where(x => x.ProjectId == projectId)
                               .ToListAsync();

    public Task<List<Deployment>> GetDeploymentsForUserAsync(UserId userId)
        => _context.Deployments
                .Where(x => x.UserId == userId)
                .ToListAsync();

    public Task<List<Deployment>> GetByProjectIdAsync(ProjectId projectId, Func<IQueryable<Deployment>, IQueryable<Deployment>> includeBuilder)
        => includeBuilder(_context.Deployments.Where(x => x.ProjectId == projectId))
            .ToListAsync();
}