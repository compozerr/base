
using System.Reflection;
using Core.Abstractions;
using Core.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Database.Data;

public static class BaseEntityWithIdEntityTypeConfigurator
{
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
                var configureMethod = configuratorType.GetMethod("Configure");

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

    public static void ConfigureAllInAssemblies(IEnumerable<Assembly> assemblies, ModelBuilder modelBuilder)
        => assemblies.Apply(assembly => ConfigureAllInAssembly(assembly, modelBuilder));
}
public class BaseEntityWithIdEntityTypeConfigurator<TId, TEntity> : IEntityTypeConfiguration<TEntity>
    where TId : IdBase<TId>, IId<TId>
    where TEntity : BaseEntityWithId<TId>
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.Id)
            .HasConversion(new IdValueConverter<TId>())
            .IsRequired();

        builder.HasKey(x => x.Id);

        // Get all properties of the entity
        var properties = typeof(TEntity).GetProperties();

        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;

            if (propertyType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IId<>)) &&
                propertyType.BaseType?.IsGenericType == true &&
                propertyType.BaseType.GetGenericTypeDefinition() == typeof(IdBase<>))
            {
                var converterType = typeof(IdValueConverter<>).MakeGenericType(propertyType);
                var converter = Activator.CreateInstance(converterType);

                var propertyBuilder = builder.Property(property.PropertyType, property.Name);

                typeof(PropertyBuilder)
                    .GetMethod(nameof(PropertyBuilder.HasConversion), [typeof(ValueConverter)])!
                    .Invoke(propertyBuilder, [converter]);
            }
        }
    }
}
