using Database.Repositories;

namespace Api.Data.Repositories;

public interface IProjectUsageRepository : IGenericRepository<ProjectUsage, ProjectUsageId, ApiDbContext>
{
    public Task AddProjectUsages(IEnumerable<ProjectUsage> projectUsages);
}

public sealed class ProjectUsageRepository(
    ApiDbContext context) : GenericRepository<ProjectUsage, ProjectUsageId, ApiDbContext>(context), IProjectUsageRepository
{
    private readonly ApiDbContext _context = context;

    public Task AddProjectUsages(IEnumerable<ProjectUsage> projectUsages)
    {
        _context.ProjectUsages.AddRange(projectUsages);
        return _context.SaveChangesAsync();
    }
}