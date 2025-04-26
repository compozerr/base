using Database.Repositories;

namespace Api.Data.Repositories;

public interface IProjectUsageRepository : IGenericRepository<ProjectUsage, ProjectUsageId, ApiDbContext>
{
    public Task AddProjectUsages(IEnumerable<ProjectUsage> projectUsages);
    public Task<List<ProjectUsage>> GetDay(ProjectId projectId);
    public Task<List<ProjectUsage>> GetWeek(ProjectId projectId);
    public Task<List<ProjectUsage>> GetMonth(ProjectId projectId);
    public Task<List<ProjectUsage>> GetYear(ProjectId projectId);
    public Task<List<ProjectUsage>> GetTotal(ProjectId projectId);
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