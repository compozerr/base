using Api.Abstractions;
using Api.Data;

namespace Api.Hosting.Services;

public sealed class MockHostingApi : IHostingApi
{
    public Task DeployAsync(Deployment deployment)
    {
        Console.WriteLine("Deployment Mock");
        return Task.CompletedTask;
    }

    public Task<ServerUsage?> GetServerUsageAsync()
    {
        var serverUsage = new ServerUsage
        {
            AvgCpuPercentage = 0.75m,
            AvgDiskPercentage = 0.60m,
            AvgRamPercentage = 0.1m
        };

        return Task.FromResult<ServerUsage?>(serverUsage);
    }

    public Task HealthCheckAsync()
    {
        Console.WriteLine("Health Check Mock");
        return Task.CompletedTask;
    }

    public Task UpdateDomainsForProjectAsync(ProjectId projectId)
    {
        Console.WriteLine($"Updating domains for project: {projectId}");
        return Task.CompletedTask;
    }
}