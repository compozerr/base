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

    public Task<ProjectUsage[]?> GetProjectsUsageAsync()
    {
        var projects = new ProjectUsage[]
        {
            new ProjectUsage
            {
                VmId = 100,
                Name = "c17e9f7a-d6d8-483a-b797-d0b967ec50c1",
                Status = "running",
                CpuUsage = 0.62m,  // Percentage
                CpuCount = 2,
                MemoryGB = 2.0m,   // 2 GB total memory
                MemoryUsedGB = 1.38m,
                FreeMemoryGB = 0.54m,
                DiskGB = 53.0m,
                DiskUsedGB = 0.0m,  // Disk usage shows 0
                NetworkInBytesPerSec = 17.23m,
                NetworkOutBytesPerSec = 6.9m,
                DiskReadBytesPerSec = 0.0m,
                DiskWriteBytesPerSec = 273.07m
            },
            new ProjectUsage
            {
                VmId = 101,
                Name = "test-project",
                Status = "running",
                CpuUsage = 1.25m,
                CpuCount = 4,
                MemoryGB = 4.0m,
                MemoryUsedGB = 2.5m,
                FreeMemoryGB = 1.5m,
                DiskGB = 80.0m,
                DiskUsedGB = 45.0m,
                NetworkInBytesPerSec = 32.5m,
                NetworkOutBytesPerSec = 18.3m,
                DiskReadBytesPerSec = 142.8m,
                DiskWriteBytesPerSec = 325.6m
            }
        };

        return Task.FromResult<ProjectUsage[]?>(projects);
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