using Auth.Data;
using Auth.Models;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Repositories;

public interface IUserRepository : IGenericRepository<User, UserId, AuthDbContext>
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByAuthProviderUserIdAsync(string authProviderUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAuthProviderUserIdAsync(string authProviderUserId, CancellationToken cancellationToken = default);
    Task<User?> GetUserWithLoginsAsync(UserId userId, CancellationToken cancellationToken = default);
}

public class UserRepository(AuthDbContext context) : GenericRepository<User, UserId, AuthDbContext>(context), IUserRepository
{
    private readonly AuthDbContext _context = context;

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByAuthProviderUserIdAsync(string authProviderUserId, CancellationToken cancellationToken = default)
        => _context.Users.Include(u => u.Logins)
                         .FirstOrDefaultAsync(u => u.Logins.Any(ul => ul.ProviderUserId == authProviderUserId), cancellationToken);

    public async Task<bool> ExistsByAuthProviderUserIdAsync(string authProviderUserId, CancellationToken cancellationToken = default)
        => await GetByAuthProviderUserIdAsync(authProviderUserId, cancellationToken) is not null;

    public Task<User?> GetUserWithLoginsAsync(UserId userId, CancellationToken cancellationToken = default)
        => _context.Users.Include(u => u.Logins)
                         .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
}