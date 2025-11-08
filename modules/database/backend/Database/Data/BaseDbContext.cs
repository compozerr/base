using Core.Abstractions;
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

        await DispatchDomainEvents_SingleInstancesAsync(cancellationToken);

        return response;
    }

    private BaseEntity[] GetEntitiesWithDomainEvents()
    {
        return [.. ChangeTracker.Entries<BaseEntity>()
                            .Select(e => e.Entity)
                            .Where(e => e.DomainEvents.Count > 0)];
    }

    private IReadOnlyList<IDomainEvent> GetDomainEventsAndRemoveWhen(DomainEventTriggerTiming when)
    {
        var entities = GetEntitiesWithDomainEvents();

        var domainEvents = new List<DomainEventWithTriggerTiming>();

        foreach (var entity in entities)
        {
            var events = entity.DomainEvents
                               .OfType<DomainEventWithTriggerTiming>()
                               .Where(e => e.When == when)
                               .ToArray();

            domainEvents.AddRange(events);

            entity.DomainEvents.RemoveAll(e => events.Contains(e));
        }

        return [.. domainEvents.Select(d => d.DomainEvent)];
    }
    private async Task DispatchDomainEvents_BeforeSaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = GetDomainEventsAndRemoveWhen(DomainEventTriggerTiming.IsBeforeSaveChanges);

        await domainEvents.ApplyAsync(domainEvent => mediator.Publish(domainEvent, cancellationToken));
    }

    private async Task DispatchDomainEvents_AfterSaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = GetDomainEventsAndRemoveWhen(DomainEventTriggerTiming.IsAfterSaveChanges);

        await domainEvents.ApplyAsync(domainEvent => mediator.Publish(domainEvent, cancellationToken));
    }

    private async Task DispatchDomainEvents_SingleInstancesAsync(CancellationToken cancellationToken = default)
    {
        var entities = GetEntitiesWithDomainEvents();

        var domainEvents = new List<IDomainEvent>();

        foreach (var entity in entities)
        {
            var events = entity.DomainEvents
                               .Where(e => e is not DomainEventWithTriggerTiming)
                               .ToArray();

            domainEvents.AddRange(events);

            entity.DomainEvents.RemoveAll(e => events.Contains(e));
        }

        await domainEvents.ApplyAsync(domainEvent => mediator.Publish(domainEvent, cancellationToken));
    }
}
