using Api.Abstractions;
using Api.Data;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public interface IProjectRepository : IGenericRepository<Project, ProjectId, ApiDbContext>
{
    public Task<Project> UpdateProjectEnvironmentVariables(KeyValuePair<string, string> pairs);
}

public sealed class ProjectRepository(
    ApiDbContext context) : GenericRepository<Project, ProjectId, ApiDbContext>(context), IProjectRepository
{
    private readonly ApiDbContext _context = context;

    public Task<Project> UpdateProjectEnvironmentVariables(KeyValuePair<string, string> pairs)
    {
        throw new NotImplementedException();
    }
}