
using Api.Data.Configurations;
using Database.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Data;
public class ApiDbContext(
    DbContextOptions<ApiDbContext> options,
    IMediator mediator) : BaseDbContext<ApiDbContext>("api", options, mediator)
{
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectEnvironment> ProjectEnvironments => Set<ProjectEnvironment>();
    public DbSet<ProjectEnvironmentVariable> ProjectEnvironmentVariables => Set<ProjectEnvironmentVariable>();
    public DbSet<ProjectUsage> ProjectUsages => Set<ProjectUsage>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<Secret> Secrets => Set<Secret>();
    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<ProjectService> ProjectServices => Set<ProjectService>();
    public DbSet<VMPool> VMPools => Set<VMPool>();
    public DbSet<VMPoolItem> VMPoolItems => Set<VMPoolItem>();
    public DbSet<TerminalSession> TerminalSessions => Set<TerminalSession>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Global converter for Protocol enum - stored as lowercase string in DB
        configurationBuilder.Properties<Protocol>()
            .HaveConversion<ProtocolConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new DomainConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectServiceConfiguration());

        modelBuilder.Entity<Server>()
            .HasOne(s => s.Secret)
            .WithOne(s => s.Server)
            .HasForeignKey<Secret>(s => s.ServerId);
    }
}

public class ProtocolConverter : ValueConverter<Protocol, string>
{
    public ProtocolConverter() : base(
        v => v.ToString().ToLowerInvariant(),
        v => ProtocolExtensions.ParseOrDefault(v))
    { }
}
