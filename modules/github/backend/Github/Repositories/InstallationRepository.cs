using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Model;

namespace Github.Repositories;

public interface IInstallationRepository : IGenericRepository<Installation, InstallationId, GithubDbContext>
{
}

public class InstallationRepository(GithubDbContext context) : GenericRepository<Installation, InstallationId, GithubDbContext>(context), IInstallationRepository
{
}
