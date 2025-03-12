using Database.Repositories;

namespace Api.Data.Repositories;

public interface ILocationRepository : IGenericRepository<Location, LocationId, ApiDbContext>;

public sealed class LocationRepository(
    ApiDbContext context) : GenericRepository<Location, LocationId, ApiDbContext>(context), ILocationRepository;