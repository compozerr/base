using Core.Abstractions;
using Core.Extensions;
using Core.Feature;
using Database.Events;
using MediatR;
using Serilog;

namespace Database.Data;

public abstract class BaseDbContext<TDbContext>(
    string schema,
    DbContextOptions options,
    IMediator mediator) : DbContext(options) where TDbContext : DbContext
{
    private static readonly AsyncLocal<bool> _isSaving = new();

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
        // Detect nested SaveChangesAsync calls (deadlock prevention)
        if (_isSaving.Value)
        {
            Log.Error(
                "Nested SaveChangesAsync detected in {DbContext}. This would cause a deadlock. " +
                "Domain event handlers should not call SaveChangesAsync. " +
                "Use HandleAfterSaveAsync for operations that need to save, or modify tracked entities without saving in HandleBeforeSaveAsync.",
                typeof(TDbContext).Name);
            throw new InvalidOperationException(
                $"Nested SaveChangesAsync detected in {typeof(TDbContext).Name}. " +
                "This would cause a deadlock. Check domain event handlers for calls that trigger SaveChangesAsync.");
        }

        _isSaving.Value = true;
        try
        {
            // Update timestamps
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

            // Dispatch events at BEFORE timing
            await DispatchDomainEventsAsync(DomainEventTiming.BeforeSaveChanges, cancellationToken);

            // Save changes to database
            var response = await base.SaveChangesAsync(cancellationToken);

            // Dispatch events at AFTER timing
            await DispatchDomainEventsAsync(DomainEventTiming.AfterSaveChanges, cancellationToken);

            return response;
        }
        finally
        {
            _isSaving.Value = false;
        }
    }

    private BaseEntity[] GetEntitiesWithDomainEvents()
    {
        return [.. ChangeTracker.Entries<BaseEntity>()
                            .Select(e => e.Entity)
                            .Where(e => e.DomainEvents.Count > 0)];
    }

    /// <summary>
    /// Dispatches domain events at the specified timing.
    ///
    /// Behavior:
    /// - IEntityDomainEvent events: Dispatched at BOTH before and after timing
    ///   (wrapped in envelope so handlers can choose which timing to handle)
    ///
    /// - IDomainEvent (non-entity) events: Dispatched ONLY at after timing
    /// </summary>
    private async Task DispatchDomainEventsAsync(
        DomainEventTiming timing,
        CancellationToken cancellationToken)
    {
        var entities = GetEntitiesWithDomainEvents();
        var eventsToDispatch = new List<IDomainEvent>();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents.ToArray())
            {
                var isEntityEvent = IsEntityDomainEvent(domainEvent);

                var shouldDispatch = timing switch
                {
                    DomainEventTiming.BeforeSaveChanges => isEntityEvent,
                    DomainEventTiming.AfterSaveChanges => true,
                    _ => false
                };

                if (shouldDispatch)
                {
                    var wrappedEvent = CreateEventEnvelope(domainEvent, timing);
                    eventsToDispatch.Add(wrappedEvent);

                    if (timing == DomainEventTiming.AfterSaveChanges)
                    {
                        entity.DomainEvents.Remove(domainEvent);
                    }
                }
            }
        }

        await eventsToDispatch.ApplyAsync(evt => mediator.Publish(evt, cancellationToken));
    }

    private static bool IsEntityDomainEvent(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();
        return eventType.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityDomainEvent<>));
    }

    private static IDomainEvent CreateEventEnvelope(IDomainEvent domainEvent, DomainEventTiming timing)
    {
        var eventType = domainEvent.GetType();
        var envelopeType = typeof(DomainEventEnvelope<>).MakeGenericType(eventType);
        var envelope = Activator.CreateInstance(envelopeType, domainEvent, timing);
        return (IDomainEvent)envelope!;
    }
}
