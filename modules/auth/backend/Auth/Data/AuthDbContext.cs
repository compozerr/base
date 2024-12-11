
using Auth.Data.Configurations;
using Auth.Models;
using Database.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data;
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : BaseDbContext(options, "auth")
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserLoginConfiguration());
    }
}
