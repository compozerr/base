using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace Core.Feature;

public class AssembliesFeatureConfigureCallback : IFeatureConfigureCallback
{
    public static IReadOnlyList<Assembly> AllDifferentAssemblies
        => [.. DifferentAssemblies];

    private static HashSet<Assembly> DifferentAssemblies { get; } = [];

    void IFeatureConfigureCallback.Configure(Type type, WebApplicationBuilder builder)
     => ((IFeatureConfigureCallback)this).Configure(type);

    void IFeatureConfigureCallback.Configure(Type type)
        => DifferentAssemblies.Add(type.Assembly);
}
