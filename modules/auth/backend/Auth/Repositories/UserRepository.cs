using Auth.Data;
using Auth.Models;
using Database.Data;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Repositories;

public interface IUserRepository : IGenericRepository<User, AuthDbContext>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetUsersWithRolesAsync();
}

public class UserRepository(AuthDbContext context) : GenericRepository<User, AuthDbContext>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetUsersWithRolesAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync();
    }
}