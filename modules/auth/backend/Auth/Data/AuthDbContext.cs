
using Auth.Data.Configurations;
using Auth.Models;
using Database.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data;
public class AuthDbContext(
    DbContextOptions<AuthDbContext> options,
    IMediator mediator) : BaseDbContext<AuthDbContext>("auth", options, mediator)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserLoginConfiguration());
    }
}
