using Auth.Abstractions;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IDeploymentRepository : IGenericRepository<Deployment, DeploymentId, ApiDbContext>
{
    public Task<List<Deployment>> GetDeploymentsForUserAsync(UserId userId);
}

public sealed class DeploymentRepository(
    ApiDbContext context) : GenericRepository<Deployment, DeploymentId, ApiDbContext>(context), IDeploymentRepository
{
    private readonly ApiDbContext _context = context;

    public Task<List<Deployment>> GetDeploymentsForUserAsync(UserId userId)
        => _context.Deployments
                .Where(x => x.UserId == userId)
                .ToListAsync();
}