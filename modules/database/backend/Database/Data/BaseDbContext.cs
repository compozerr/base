using Core.Extensions;
using Core.Feature;
using MediatR;

namespace Database.Data;


public abstract class BaseDbContext<TDbContext>(
    string schema,
    DbContextOptions options,
    IMediator mediator) : DbContext(options) where TDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (!string.IsNullOrEmpty(schema))
        {
            modelBuilder.HasDefaultSchema(schema);
        }

        BaseEntityWithIdEntityTypeConfigurator.ConfigureAllInAssemblies(AssembliesFeatureConfigureCallback.AllDifferentAssemblies, modelBuilder);
        IgnoreEntitiesInOtherContexts(modelBuilder);
    }

    private static string GetNamespaceRoot(string ns)
    {
        var parts = ns.Split(".");
        return parts.Length > 1 ? parts[0] : ns;
    }

    private static void IgnoreEntitiesInOtherContexts(ModelBuilder modelBuilder)
    {
        var entities = modelBuilder.Model.GetEntityTypes().ToList();
        foreach (var entity in entities)
        {
            if (entity.ClrType.Namespace is not { } nonNullEntityNamespace
                || typeof(TDbContext).Namespace is not { } nonNullDbContextNamespace)
                continue;

            if (GetNamespaceRoot(nonNullEntityNamespace) != GetNamespaceRoot(nonNullDbContextNamespace))
            {
                modelBuilder.Ignore(entity.ClrType);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
                                   .Select(e => e.Entity)
                                   .Where(e => e.DomainEvents.Count > 0)
                                   .ToArray();

        foreach (var entity in entries)
        {
            var events = entity.DomainEvents.ToArray();
            entity.DomainEvents.Clear();

            await events.ApplyAsync(domainEvent => mediator.Publish(domainEvent, cancellationToken));
        }
    }
}
