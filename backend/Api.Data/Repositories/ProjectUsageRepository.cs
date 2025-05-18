using Database.Repositories;
using Microsoft.EntityFrameworkCore;

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
    private const int MaxDataPoints = 50;

    public Task AddProjectUsages(IEnumerable<ProjectUsage> projectUsages)
        => AddRangeAsync(projectUsages);

    public async Task<List<ProjectUsage>> GetDay(ProjectId projectId)
    {
        var startDate = DateTime.UtcNow.Date;
        // For a day, we'll aggregate by minutes
        var interval = TimeSpan.FromMinutes(24 * 60 / MaxDataPoints);

        return await GetAggregatedUsage(projectId, startDate, interval);
    }

    public async Task<List<ProjectUsage>> GetWeek(ProjectId projectId)
    {
        var startDate = DateTime.UtcNow.AddDays(-7).Date;
        // For a week, we'll aggregate by hours
        var interval = TimeSpan.FromHours(7 * 24 / MaxDataPoints);

        return await GetAggregatedUsage(projectId, startDate, interval);
    }

    public async Task<List<ProjectUsage>> GetMonth(ProjectId projectId)
    {
        var startDate = DateTime.UtcNow.AddMonths(-1).Date;
        // For a month, we'll aggregate by days
        var interval = TimeSpan.FromHours(30 * 24 / MaxDataPoints);

        return await GetAggregatedUsage(projectId, startDate, interval);
    }

    public async Task<List<ProjectUsage>> GetYear(ProjectId projectId)
    {
        var startDate = DateTime.UtcNow.AddYears(-1).Date;
        // For a year, we'll aggregate by weeks
        var interval = TimeSpan.FromDays(365 / MaxDataPoints);

        return await GetAggregatedUsage(projectId, startDate, interval);
    }

    public async Task<List<ProjectUsage>> GetTotal(ProjectId projectId)
    {
        var startDate = await Query()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        // If no records exist yet, use current time to avoid issues
        if (startDate == default)
        {
            return new List<ProjectUsage>();
        }

        var endDate = DateTime.UtcNow;
        var totalDays = (endDate - startDate).TotalDays;
        var interval = TimeSpan.FromDays(Math.Max(1, totalDays / MaxDataPoints));

        return await GetAggregatedUsage(projectId, startDate, interval);
    }

    private async Task<List<ProjectUsage>> GetAggregatedUsage(ProjectId projectId, DateTime startDate, TimeSpan interval)
    {
        var results = await Query()
            .Where(x => x.ProjectId == projectId && x.CreatedAtUtc >= startDate)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        return AggregateByInterval(results, startDate, interval);
    }

    private List<ProjectUsage> AggregateByInterval(List<ProjectUsage> usages, DateTime startDate, TimeSpan interval)
    {
        if (!usages.Any())
            return new List<ProjectUsage>();

        var aggregated = new List<ProjectUsage>();
        var endDate = DateTime.UtcNow;

        for (var intervalStart = startDate; intervalStart < endDate; intervalStart += interval)
        {
            var intervalEnd = intervalStart + interval;
            var usagesInInterval = usages.Where(u => u.CreatedAtUtc >= intervalStart && u.CreatedAtUtc < intervalEnd).ToList();

            if (usagesInInterval.Any())
            {
                // Create a new ProjectUsage object for the aggregated data
                // Use the middle of the interval for timestamp to better represent the interval
                var midPoint = intervalStart.AddTicks(interval.Ticks / 2);
                var firstUsage = usagesInInterval.First();
                var count = usagesInInterval.Count;

                var aggregatedUsage = new ProjectUsage
                {
                    Id = new ProjectUsageId(Guid.NewGuid()),
                    ProjectId = firstUsage.ProjectId,
                    VmId = firstUsage.VmId,
                    Name = firstUsage.Name,
                    Status = firstUsage.Status,

                    // Average for percentage/utilization metrics
                    CpuUsagePercentage = usagesInInterval.Average(u => u.CpuUsagePercentage),
                    CpuCount = usagesInInterval.Max(u => u.CpuCount), // Take max for resource capacity
                    MemoryUsageGb = usagesInInterval.Average(u => u.MemoryUsageGb),
                    TotalMemoryGb = usagesInInterval.Max(u => u.TotalMemoryGb), // Take max for resource capacity
                    DiskUsageGb = usagesInInterval.Average(u => u.DiskUsageGb),
                    TotalDiskGb = usagesInInterval.Max(u => u.TotalDiskGb), // Take max for resource capacity

                    // Average for rate metrics
                    NetworkInBytesPerSec = usagesInInterval.Average(u => u.NetworkInBytesPerSec),
                    NetworkOutBytesPerSec = usagesInInterval.Average(u => u.NetworkOutBytesPerSec),
                    DiskReadBytesPerSec = usagesInInterval.Average(u => u.DiskReadBytesPerSec),
                    DiskWriteBytesPerSec = usagesInInterval.Average(u => u.DiskWriteBytesPerSec),

                    // Set created timestamp to represent this interval
                    CreatedAtUtc = midPoint,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                aggregated.Add(aggregatedUsage);
            }
        }

        return aggregated.OrderBy(x => x.CreatedAtUtc).Take(MaxDataPoints).ToList();
    }
}