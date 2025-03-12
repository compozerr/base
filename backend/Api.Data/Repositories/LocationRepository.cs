using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface ILocationRepository : IGenericRepository<Location, LocationId, ApiDbContext>
{
    Task<Location> GetLocationByIso(string iso);
};

public sealed class LocationRepository(
    ApiDbContext context) : GenericRepository<Location, LocationId, ApiDbContext>(context), ILocationRepository
{
    private readonly ApiDbContext _context = context;

    public Task<Location> GetLocationByIso(string iso)
        => _context.Locations.SingleAsync(x => x.IsoCountryCode == iso);
};