
using System.Reflection;
using Core.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Data;

public static class BaseEntityWithIdEntityTypeConfigurator
{
    public static BaseEntityWithIdEntityTypeConfigurator<TId, TEntity> Create<TId, TEntity>()
        where TId : IdBase<TId>, IId<TId>
        where TEntity : BaseEntityWithId<TId>
        => new();

    public static void Configure<TId, TEntity>(EntityTypeBuilder<TEntity> builder)
        where TId : IdBase<TId>, IId<TId>
        where TEntity : BaseEntityWithId<TId>
        => Create<TId, TEntity>().Configure(builder);

    public static void ConfigureAllInAssembly(Assembly assembly, ModelBuilder modelBuilder)
    {
        var types = assembly.GetTypes();

        var entityTypes = types
            .Where(t => t.BaseType != null &&
                        t.BaseType.IsGenericType &&
                        t.BaseType.GetGenericTypeDefinition() == typeof(BaseEntityWithId<>));

        foreach (var concreteEntityType in entityTypes)
        {
            var idType = concreteEntityType.BaseType!.GetGenericArguments()[0];

            var configuratorType = typeof(BaseEntityWithIdEntityTypeConfigurator<,>)
                .MakeGenericType(idType, concreteEntityType);

            var configurator = Activator.CreateInstance(configuratorType);

            if (configurator != null)
            {
                var configureMethod = configuratorType.GetMethod(nameof(Configure));

                var builderType = typeof(EntityTypeBuilder<>).MakeGenericType(concreteEntityType);

                var genericBuilder = typeof(ModelBuilder)
                    .GetMethod(nameof(ModelBuilder.Entity), Type.EmptyTypes)!
                    .MakeGenericMethod(concreteEntityType)
                    .Invoke(modelBuilder, null);

                if (configureMethod != null && genericBuilder != null)
                {
                    configureMethod.Invoke(configurator, [genericBuilder]);
                }
            }
        }
    }
}
public class BaseEntityWithIdEntityTypeConfigurator<TId, TEntity> : IEntityTypeConfiguration<TEntity>
    where TId : IdBase<TId>, IId<TId>
    where TEntity : BaseEntityWithId<TId>
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(typeof(TEntity).Name);

        builder.Property(x => x.Id)
            .HasConversion(new IdValueConverter<TId>())
            .IsRequired();

        builder.HasKey(x => x.Id);
    }
}
