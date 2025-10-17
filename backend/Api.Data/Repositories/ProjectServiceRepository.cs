using Database.Repositories;

namespace Api.Data.Repositories;

public interface IProjectServiceRepository : IGenericRepository<ProjectService, ProjectServiceId, ApiDbContext>
{
}

public sealed class ProjectServiceRepository(
    ApiDbContext context) : GenericRepository<ProjectService, ProjectServiceId, ApiDbContext>(context), IProjectServiceRepository
{
}