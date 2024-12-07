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
}

public class UserRepository(AuthDbContext context) : GenericRepository<User, UserId, AuthDbContext>(context), IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByAuthProviderUserIdAsync(string authProviderUserId, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.AuthProviderUserId == authProviderUserId, cancellationToken);

    public Task<bool> ExistsByAuthProviderUserIdAsync(string authProviderUserId, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(u => u.AuthProviderUserId == authProviderUserId, cancellationToken);
}