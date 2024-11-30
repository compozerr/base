using Auth.Abstractions;
using Auth.Data;
using Auth.Models;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Repositories;

public interface IUserRepository : IGenericRepository<User, UserId, AuthDbContext>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateUser(User user);
    Task<IEnumerable<User>> GetUsersWithRolesAsync();
}

public class UserRepository(AuthDbContext context) : GenericRepository<User, UserId, AuthDbContext>(context), IUserRepository
{
    public Task<User> CreateUser(User user)
    {
        throw new NotImplementedException();
    }

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