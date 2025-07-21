using Core.Extensions;
using Core.Feature;
using Database.Events;
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
            .Where(e => (e.Entity is BaseEntity || e.Entity.GetType().IsSubclassOf(typeof(BaseEntity))) && (
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

        await DispatchDomainEvents_BeforeSaveChangesAsync(cancellationToken);

        var response = await base.SaveChangesAsync(cancellationToken);

        await DispatchDomainEvents_AfterSaveChangesAsync(cancellationToken);

        return response;
    }

    private BaseEntity[] GetEntitiesWithDomainEvents()
    {
        return [.. ChangeTracker.Entries<BaseEntity>()
                            .Select(e => e.Entity)
                            .Where(e => e.DomainEvents.Count > 0)];
    }

    private async Task DispatchDomainEvents_BeforeSaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = GetEntitiesWithDomainEvents();

        foreach (var entity in entries)
        {
            var events = entity.DomainEvents.Where(e => e is IDispatchBeforeSaveChanges).ToArray();
            entity.DomainEvents.RemoveAll(e => e is IDispatchBeforeSaveChanges);

            await events.ApplyAsync(domainEvent => mediator.Publish(domainEvent, cancellationToken));
        }
    }

    private async Task DispatchDomainEvents_AfterSaveChangesAsync(CancellationToken cancellationToken = default)
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
