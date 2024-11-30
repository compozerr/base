using Core.Abstractions;
using Core.Feature;

namespace Database.Data;

public class RegisterIdTypeConfiguratorsFeatureConfigureCallback : IFeatureConfigureCallback
{
    private readonly static List<IEntityTypeConfiguration<Type>> _entityTypeConfigurators = [];

    public static IReadOnlyList<IEntityTypeConfiguration<Type>> EntityTypeConfigurators => _entityTypeConfigurators
                                                                                           ?? throw new InvalidOperationException("Ids have not been initialized.");

    void IFeatureConfigureCallback.Configure(Type type, Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
    {
        var assemblyTypes = type.Assembly.GetTypes();

        var idBaseTypes = assemblyTypes
            .Where(x => x.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IId<>)))
            .ToList();

        var entityTypeConfiguratorType = typeof(BaseEntityWithIdEntityTypeConfigurator<,>);

        foreach (var idBaseType in idBaseTypes)
        {
            var entityTypeConfigurator = entityTypeConfiguratorType.MakeGenericType(idBaseType, typeof(BaseEntityWithId<>).MakeGenericType(idBaseType));
            Console.WriteLine("Registering entity type configurator: {0}", entityTypeConfigurator.FullName);
            _entityTypeConfigurators.Add((IEntityTypeConfiguration<Type>)Activator.CreateInstance(entityTypeConfigurator)!);
        }
    }
}