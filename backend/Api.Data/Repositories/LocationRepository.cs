using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface ILocationRepository : IGenericRepository<Location, LocationId, ApiDbContext>
{
    Task<Location> GetLocationByIso(string iso);
    Task<List<string>> GetUniquePublicLocationIsoCodes();
};

public sealed class LocationRepository(
    ApiDbContext context) : GenericRepository<Location, LocationId, ApiDbContext>(context), ILocationRepository
{
    private readonly ApiDbContext _context = context;

    public Task<Location> GetLocationByIso(string iso)
        => Query().SingleAsync(x => x.IsoCountryCode == iso);

    public async Task<List<string>> GetUniquePublicLocationIsoCodes()
    {
        var uniquePublicServerLocations = await _context.Servers
            .Where(x => x.ServerVisibility == ServerVisibility.Public && x.LocationId != null && x.DeletedAtUtc == null)
            .Select(x => x.LocationId)
            .Distinct()
            .ToListAsync();

        return await Query()
            .Where(x => uniquePublicServerLocations.Contains(x.Id))
            .Select(x => x.IsoCountryCode)
            .ToListAsync();
    }
};