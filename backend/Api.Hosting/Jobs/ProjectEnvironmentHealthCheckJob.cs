using System.Collections.Concurrent;
using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Jobs;
using Serilog;

namespace Api.Hosting.Jobs;

public class ProjectEnvironmentHealthCheckJob(
    IProjectEnvironmentRepository environmentRepository,
    IHostingApiFactory hostingApiFactory) : JobBase<ProjectEnvironmentHealthCheckJob>
{
    private const int UnhealthyFlipThreshold = 2;

    public override async Task ExecuteAsync()
    {
        var environments = await environmentRepository.GetAllActiveWithServerAsync();

        var hostingApiCache = new ConcurrentDictionary<ServerId, IHostingApi?>();
        // Per-env ServerId is the source of truth post-Spec-B: envs of the
        // same project may live on different servers, so grouping by env's
        // own ServerId keeps each batch routed to the right Traefik.
        var byServer = environments
            .Where(e => e.ServerId is not null)
            .GroupBy(e => e.ServerId!);

        var probeTasks = byServer.Select(async group =>
        {
            var api = await GetOrResolveHostingApiAsync(hostingApiCache, group.Key);
            if (api is null) return Array.Empty<(ProjectEnvironment env, EnvironmentHealthResponse? response)>();

            var results = await Task.WhenAll(group.Select(async env =>
                (env, response: await api.GetEnvironmentHealthAsync(env.ProjectId, env.Id))));
            return results;
        });

        var allResults = (await Task.WhenAll(probeTasks)).SelectMany(x => x);

        foreach (var (env, response) in allResults)
        {
            if (response is null) continue;
            if (ApplyHealthResponse(env, response))
            {
                await environmentRepository.UpdateAsync(env);
            }
        }
    }

    private async Task<IHostingApi?> GetOrResolveHostingApiAsync(
        ConcurrentDictionary<ServerId, IHostingApi?> cache, ServerId serverId)
    {
        if (cache.TryGetValue(serverId, out var cached)) return cached;

        try
        {
            var api = await hostingApiFactory.GetHostingApiAsync(serverId);
            cache[serverId] = api;
            return api;
        }
        catch (Exception ex)
        {
            Log.Logger.ForContext("serverId", serverId).ForContext("error", ex)
                .Warning("Could not resolve hosting API; skipping envs on this server this tick");
            cache[serverId] = null;
            return null;
        }
    }

    /// <summary>
    /// Updates the env's health fields. Returns true when a DB write is needed
    /// (status or failure counter changed); returns false for no-op ticks.
    /// </summary>
    private static bool ApplyHealthResponse(ProjectEnvironment env, EnvironmentHealthResponse response)
    {
        var prevStatus = env.HealthStatus;
        var prevFailures = env.ConsecutiveHealthFailures;

        switch (response.Status)
        {
            case EnvironmentHealthReportStatus.Healthy:
                env.HealthStatus = EnvironmentHealthStatus.Healthy;
                env.ConsecutiveHealthFailures = 0;
                break;

            case EnvironmentHealthReportStatus.Unhealthy:
                env.ConsecutiveHealthFailures += 1;
                if (env.ConsecutiveHealthFailures >= UnhealthyFlipThreshold)
                    env.HealthStatus = EnvironmentHealthStatus.Unhealthy;
                break;

            case EnvironmentHealthReportStatus.Unknown:
                env.HealthStatus = EnvironmentHealthStatus.Unknown;
                env.ConsecutiveHealthFailures = 0;
                break;
        }

        var changed = env.HealthStatus != prevStatus || env.ConsecutiveHealthFailures != prevFailures;
        if (changed) env.LastHealthCheckAtUtc = DateTimeOffset.UtcNow;
        return changed;
    }
}
