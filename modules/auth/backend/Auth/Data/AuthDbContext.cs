
using Auth.Models;
using Database.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data;
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : BaseDbContext(options, "auth")
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        BaseEntityWithIdEntityTypeConfigurator.ConfigureAllInAssembly(typeof(User).Assembly, modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
