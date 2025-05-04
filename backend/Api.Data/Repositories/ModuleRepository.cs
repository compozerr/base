using Database.Repositories;

namespace Api.Data.Repositories;

public interface IModuleRepository : IGenericRepository<Module, ModuleId, ApiDbContext>;

public sealed class ModuleRepository(
    ApiDbContext context) : GenericRepository<Module, ModuleId, ApiDbContext>(context), IModuleRepository;