using Database.Repositories;

namespace Api.Data.Repositories;

public interface IProjectEnvironmentRepository : IGenericRepository<ProjectEnvironment, ProjectEnvironmentId, ApiDbContext>;

public sealed class ProjectEnvironmentRepository(
    ApiDbContext context) : GenericRepository<ProjectEnvironment, ProjectEnvironmentId, ApiDbContext>(context), IProjectEnvironmentRepository;