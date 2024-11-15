using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Feature;

public static class Features
{
    private static Lazy<IReadOnlyList<IFeature>> AllFeatures { get; } = new(GetFeaturesByReflection);
    private static IReadOnlyList<IFeature> GetFeaturesByReflection()
    {
        var featureType = typeof(IFeature);
        var types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => featureType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        return [.. types.Select(Activator.CreateInstance).Cast<IFeature>()];
    }

    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        foreach (var feature in AllFeatures.Value)
        {
            feature.ConfigureServices(services);
        }

        return services;
    }

    public static WebApplication UseFeatures(this WebApplication app)
    {
        foreach (var feature in AllFeatures.Value)
        {
            feature.ConfigureApp(app);
        }

        return app;
    }
}