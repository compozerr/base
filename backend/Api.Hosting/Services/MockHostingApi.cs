using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Dtos;

namespace Api.Hosting.Services;

public sealed class MockHostingApi(IProjectRepository projectRepository) : IHostingApi
{
    public Task DeployAsync(Deployment deployment)
    {
        Console.WriteLine("Deployment Mock");
        return Task.CompletedTask;
    }

    public async Task<ProjectUsageDto[]?> GetProjectsUsageAsync()
    {
        var allProjects = await projectRepository.GetAllAsync();

        int index = 100;

        var random = new Random();

        var projects = allProjects.Select(p =>
        {
            decimal availableMemory = 2.0m;
            decimal memoryUsed = (decimal)random.NextDouble() * availableMemory;
            decimal freemem = availableMemory - memoryUsed;
            return new ProjectUsageDto
            {
                VmId = index++,
                Name = p.Id.Value.ToString(),
                Status = "running",
                CpuUsage = (decimal)random.NextDouble(),
                CpuCount = 2,
                MemoryGB = availableMemory,
                MemoryUsedGB = memoryUsed,
                FreeMemoryGB = freemem,
                DiskGB = 50.0m,
                DiskUsedGB = 0.0m,
                NetworkInBytesPerSec = (decimal)random.NextDouble() * 50,
                NetworkOutBytesPerSec = (decimal)random.NextDouble() * 20,
                DiskReadBytesPerSec = (decimal)random.NextDouble() * 300,
                DiskWriteBytesPerSec = (decimal)random.NextDouble() * 300,
            };
        }).ToArray();

        return projects;
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