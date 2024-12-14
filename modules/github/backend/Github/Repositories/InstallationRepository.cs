using Auth.Abstractions;
using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Model;
using Microsoft.EntityFrameworkCore;

namespace Github.Repositories;

public interface IInstallationRepository : IGenericRepository<Installation, InstallationId, GithubDbContext>
{
    Task<List<Installation>> GetInstallationsByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<InstallationId> AddInstallationAsync(UserId userId, string accessToken, string scope, CancellationToken cancellationToken = default);
}

public class InstallationRepository(GithubDbContext context) : GenericRepository<Installation, InstallationId, GithubDbContext>(context), IInstallationRepository
{
    private readonly GithubDbContext _context = context;

    public async Task<InstallationId> AddInstallationAsync(UserId userId, string accessToken, string scope, CancellationToken cancellationToken = default)
    {
        var installation = new Installation
        {
            UserId = userId,
            AccessToken = accessToken,
            Scope = scope
        };

        var addedInstallation = await AddAsync(installation, cancellationToken);

        return addedInstallation.Id;
    }

    public Task<List<Installation>> GetInstallationsByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
        => _context.Installations.Where(i => i.UserId == userId).ToListAsync(cancellationToken);
}
