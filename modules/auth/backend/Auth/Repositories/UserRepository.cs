using Auth.Abstractions;
using Auth.Data;
using Auth.Models;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Auth.Repositories;

public interface IUserRepository : IGenericRepository<User, UserId, AuthDbContext>
{
    Task<User?> GetByEmailAsync(string email);
}

public class UserRepository(AuthDbContext context) : GenericRepository<User, UserId, AuthDbContext>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }
}