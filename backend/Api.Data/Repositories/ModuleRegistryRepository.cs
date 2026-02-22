using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IModuleRegistryRepository : IGenericRepository<ModuleRegistryEntry, ModuleRegistryEntryId, ApiDbContext>
{
    Task<(List<ModuleRegistryEntry> Entries, int TotalCount)> SearchAsync(
        string? query, string[]? tags, int limit, int offset);

    Task<ModuleRegistryEntry?> GetByOrgAndNameAsync(string organization, string name);

    Task IncrementInstallCountAsync(ModuleRegistryEntryId id);
}

public sealed class ModuleRegistryRepository(
    ApiDbContext context) : GenericRepository<ModuleRegistryEntry, ModuleRegistryEntryId, ApiDbContext>(context), IModuleRegistryRepository
{
    private readonly ApiDbContext _context = context;

    public async Task<(List<ModuleRegistryEntry> Entries, int TotalCount)> SearchAsync(
        string? query, string[]? tags, int limit, int offset)
    {
        var q = Query()
            .Include(e => e.Versions.OrderByDescending(v => v.CreatedAtUtc).Take(1))
            .Where(e => e.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLower();
            q = q.Where(e =>
                e.Name.ToLower().Contains(lower) ||
                (e.Description != null && e.Description.ToLower().Contains(lower)) ||
                e.Organization.ToLower().Contains(lower));
        }

        if (tags is { Length: > 0 })
        {
            q = q.Where(e => e.Tags.Any(t => tags.Contains(t)));
        }

        var totalCount = await q.CountAsync();

        var entries = await q
            .OrderByDescending(e => e.InstallCount)
            .ThenByDescending(e => e.UpdatedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (entries, totalCount);
    }

    public Task<ModuleRegistryEntry?> GetByOrgAndNameAsync(string organization, string name)
    {
        return Query()
            .Include(e => e.Versions.OrderByDescending(v => v.CreatedAtUtc))
            .FirstOrDefaultAsync(e =>
                e.Organization.ToLower() == organization.ToLower() &&
                e.Name.ToLower() == name.ToLower() &&
                e.DeletedAtUtc == null);
    }

    public async Task IncrementInstallCountAsync(ModuleRegistryEntryId id)
    {
        await _context.ModuleRegistryEntries
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.InstallCount, e => e.InstallCount + 1));
    }
}
