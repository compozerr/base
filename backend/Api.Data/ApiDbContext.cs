
using Database.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;
public class ApiDbContext(
    DbContextOptions<ApiDbContext> options,
    IMediator mediator) : BaseDbContext<ApiDbContext>("api", options, mediator)
{
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Server> Servers => Set<Server>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
