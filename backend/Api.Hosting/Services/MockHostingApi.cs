using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Dtos;

namespace Api.Hosting.Services;

public sealed class MockHostingApi(IProjectRepository projectRepository) : IHostingApi
{
    public Task DeleteProjectAsync(ProjectId projectId)
    {
        Console.WriteLine("[MOCK] Deleting project");
        return Task.CompletedTask;
    }

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
            var status = random.NextDouble() > 0.1 ? "running" : "stopped";

            decimal availableMemory = 2.0m;
            decimal memoryUsed = status == "running" ? (decimal)random.NextDouble() * availableMemory : 0.0m;
            decimal freemem = availableMemory - memoryUsed;

            return new ProjectUsageDto
            {
                VmId = index++,
                Name = p.Id.Value.ToString(),
                Status = status,
                CpuUsage = status == "running" ? (decimal)random.NextDouble() : 0.0m,
                CpuCount = 2,
                MemoryGB = availableMemory,
                MemoryUsedGB = memoryUsed,
                FreeMemoryGB = freemem,
                DiskGB = 50.0m,
                DiskUsedGB = 0.0m,
                NetworkInBytesPerSec = status == "running" ? (decimal)random.NextDouble() * 50 : 0,
                NetworkOutBytesPerSec = status == "running" ? (decimal)random.NextDouble() * 20 : 0,
                DiskReadBytesPerSec = status == "running" ? (decimal)random.NextDouble() * 300 : 0,
                DiskWriteBytesPerSec = status == "running" ? (decimal)random.NextDouble() * 300 : 0,
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

    public async Task StartProjectAsync(ProjectId projectId)
    {
        await projectRepository.SetProjectStateAsync(projectId, ProjectState.Running);
    }

    public async Task StopProjectAsync(ProjectId projectId)
    {
        await projectRepository.SetProjectStateAsync(projectId, ProjectState.Stopped);
    }

    public Task UpdateDomainsForProjectAsync(ProjectId projectId)
    {
        Console.WriteLine($"Updating domains for project: {projectId}");
        return Task.CompletedTask;
    }
}