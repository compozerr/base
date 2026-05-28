using Api.Data;
using Core.Feature;

namespace Api.Tests;

/// <summary>
/// Shared bootstrap that registers Api.Data with
/// <see cref="AssembliesFeatureConfigureCallback"/> exactly once across all
/// test classes. The callback's backing collection is a non-thread-safe
/// <see cref="HashSet{T}"/>, so two test classes initializing it from their
/// static constructors in parallel corrupts the set ("Operations that change
/// non-concurrent collections must have exclusive access").
/// </summary>
internal static class ApiDbContextTestBootstrap
{
    private static readonly object Gate = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (Gate)
        {
            if (_initialized) return;
            var callback = (IFeatureConfigureCallback)new AssembliesFeatureConfigureCallback();
            callback.Configure(typeof(ProjectEnvironment));
            callback.Configure(typeof(ApiDbContext));
            _initialized = true;
        }
    }
}
