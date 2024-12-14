using Core.Feature;

namespace Database.Data;


public abstract class BaseDbContext<TDbContext> : DbContext where TDbContext : DbContext
{
    protected readonly string _schema;

    protected BaseDbContext(DbContextOptions options, string schema) : base(options)
    {
        _schema = schema;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (!string.IsNullOrEmpty(_schema))
        {
            modelBuilder.HasDefaultSchema(_schema);
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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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

        return base.SaveChangesAsync(cancellationToken);
    }
}
