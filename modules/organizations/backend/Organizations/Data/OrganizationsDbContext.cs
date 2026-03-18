using Database.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Organizations.Models;

namespace Organizations.Data;

public class OrganizationsDbContext(
    DbContextOptions<OrganizationsDbContext> options,
    IMediator mediator) : BaseDbContext<OrganizationsDbContext>("organizations", options, mediator)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new Configurations.OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrganizationMemberConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrganizationInvitationConfiguration());
    }
}
