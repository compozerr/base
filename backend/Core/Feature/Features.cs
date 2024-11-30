using System.Reflection;
using Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
                _allFeatures = GetFeaturesFromDlls();
            }
            return _allFeatures;
        }
    }

    private static readonly List<IFeatureConfigureCallback> _callbacks = new();

    private static IReadOnlyList<IFeature> GetFeaturesFromDlls()
    {
        // Locate the output directory
        var outputDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Get all DLL files in the output directory
        var dllFiles = Directory.GetFiles(outputDirectory, "*.dll");

        var loadedAssemblies = new List<Assembly>();

        foreach (var dllFile in dllFiles)
        {
            try
            {
                // Load assembly from file
                var assembly = Assembly.LoadFrom(dllFile);
                loadedAssemblies.Add(assembly);

                // Log.Logger.Information("Successfully loaded assembly: {AssemblyName}", assembly.GetName().Name);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Failed to load assembly {DllFile}: {ExceptionMessage}", dllFile, ex.Message);
            }
        }

        // Find all types implementing IFeature
        var featureType = typeof(IFeature);
        var featureTypes = loadedAssemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => featureType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        Log.Logger.Information("Found {FeatureCount} features", featureTypes.Count());

        // Instantiate and return features
        return [.. featureTypes.Select(Activator.CreateInstance).Cast<IFeature>()];
    }

    public static WebApplicationBuilder ConfigureFeatures(this WebApplicationBuilder builder)
    {
        AllFeatures.Apply(f => f.ConfigureBuilder(builder));
        AllFeatures.Apply(f => _callbacks.Apply(c => c.Configure(f.GetType(), builder)));

        return builder;
    }

    public static IServiceCollection AddFeatures(this IServiceCollection services, IConfiguration configuration)
    {
        AllFeatures.Apply(f => f.ConfigureServices(services));
        AllFeatures.Apply(f => f.ConfigureServices(services, configuration));

        return services;
    }

    public static WebApplication UseFeatures(this WebApplication app)
    {
        AllFeatures.Apply(f =>
        {
            Log.Logger.Information("Configuring feature {Feature}", f.GetType().Name);
            f.ConfigureApp(app);
        });

        AllFeatures.Apply(f => _callbacks.Apply(c => c.Configure(f.GetType(), app)));

        return app;
    }

    public static void RegisterConfigureCallback(IFeatureConfigureCallback callback)
        => _callbacks.Add(callback);

    public static void RegisterConfigureCallback<TCallback>()
        where TCallback : IFeatureConfigureCallback, new()
        => RegisterConfigureCallback(new TCallback());
}
