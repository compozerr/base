using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Core.Feature;

public static class Features
{
    private static IReadOnlyList<IFeature>? _allFeatures;
    private static IReadOnlyList<IFeature> AllFeatures
    {
        get
        {
            if (_allFeatures is null)
            {
                _allFeatures = GetFeaturesByReflection();
            }
            return _allFeatures;
        }
    }

    private static IReadOnlyList<IFeature> GetFeaturesByReflection()
    {
        var apiAssembly = Assembly.Load(GetCallingAssemblyName());

        Log.Logger.Information("Loading features from assembly {AssemblyName}", apiAssembly.GetName().Name);

        var allReferencedAssemblies = apiAssembly.GetReferencedAssemblies();

        Log.Logger.Information("Found {ReferencedAssembliesCount} referenced assemblies, names: {Names}", allReferencedAssemblies.Length, allReferencedAssemblies.Select(assembly => assembly.Name));

        var allAssemblies = allReferencedAssemblies.Select(Assembly.Load).Append(apiAssembly);

        var featureType = typeof(IFeature);
        var types = allAssemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => featureType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        Log.Logger.Information("Found {FeatureCount} features", types.Count());

        return [.. types.Select(Activator.CreateInstance).Cast<IFeature>()];
    }

    private static string GetCallingAssemblyName()
    {
        var stackTrace = new StackTrace();
        var callingAssembly = stackTrace.GetFrames()
            .Select(frame => frame.GetMethod()?.DeclaringType?.Assembly)
            .FirstOrDefault(assembly => assembly?.GetName().Name != "Core");

        return callingAssembly?.GetName().Name ?? throw new InvalidOperationException("Calling assembly not found");
    }

    public static WebApplicationBuilder ConfigureFeatures(this WebApplicationBuilder builder)
    {
        foreach (var feature in AllFeatures)
        {
            feature.ConfigureBuilder(builder);
        }

        return builder;
    }

    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        foreach (var feature in AllFeatures)
        {
            feature.ConfigureServices(services);
        }

        return services;
    }

    public static WebApplication UseFeatures(this WebApplication app)
    {
        foreach (var feature in AllFeatures)
        {
            feature.ConfigureApp(app);
        }

        return app;
    }

    public static void AppendFeatureRoutes(this IEndpointRouteBuilder routeBuilder)
    {
        foreach (var feature in AllFeatures)
        {
            Log.Logger.Information("Adding routes for feature {Feature}", feature.GetType().Name);

            feature.AddRoutes(routeBuilder);
        }
    }
}