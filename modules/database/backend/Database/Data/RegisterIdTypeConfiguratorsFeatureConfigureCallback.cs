using Core.Abstractions;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Data;

public class RegisterIdTypeConfiguratorsFeatureConfigureCallback : IFeatureConfigureCallback
{
    // Changed to use a non-generic interface for storing configurations
    private readonly static List<object> _entityTypeConfigurators = [];

    // Modified to return the concrete type configurations
    public static IReadOnlyList<object> EntityTypeConfigurators => _entityTypeConfigurators
        ?? throw new InvalidOperationException("Ids have not been initialized.");

    void IFeatureConfigureCallback.Configure(Type type, WebApplicationBuilder builder)
    {
        var assemblyTypes = type.Assembly.GetTypes();

        var idBaseTypes = assemblyTypes
            .Where(x => x.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IId<>)))
            .ToList();

        var entityTypeConfiguratorType = typeof(BaseEntityWithIdEntityTypeConfigurator<,>);

        foreach (var idBaseType in idBaseTypes)
        {
            var entityType = typeof(BaseEntityWithId<>).MakeGenericType(idBaseType);
            var configuratorType = entityTypeConfiguratorType.MakeGenericType(idBaseType, entityType);

            Console.WriteLine($"Registering entity type configurator: {configuratorType.FullName}");

            // Create the configurator instance without casting to IEntityTypeConfiguration<Type>
            var configurator = Activator.CreateInstance(configuratorType);
            if (configurator != null)
            {
                _entityTypeConfigurators.Add(configurator);
            }
        }
    }

    // Add a helper method to apply configurations to DbContext
    public static void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        foreach (var configurator in EntityTypeConfigurators)
        {
            // Get the concrete configuration type
            var configuratorType = configurator.GetType();

            // Find the entity type from the IEntityTypeConfiguration interface
            var configurationInterface = configuratorType
                .GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

            var entityType = configurationInterface.GetGenericArguments()[0];

            // Use reflection to call the Configure method
            var configureMethod = configuratorType.GetMethod("Configure",
                new[] { typeof(EntityTypeBuilder<>).MakeGenericType(entityType) });

            var entityTypeBuilder = modelBuilder.Entity(entityType);
            configureMethod?.Invoke(configurator, new[] { entityTypeBuilder });
        }
    }
}