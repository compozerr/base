using Api.Abstractions;
using Api.Data;
using Api.Hosting.Dtos;

namespace Api.Hosting.Services;

public interface IHostingApi
{
    Task DeployAsync(Deployment deployment);
    Task<ServerUsage?> GetServerUsageAsync();
    Task<ProjectUsageDto[]?> GetProjectsUsageAsync();
    Task HealthCheckAsync();
    Task UpdateDomainsForProjectAsync(ProjectId projectId);
    Task StartProjectAsync(ProjectId projectId);
    Task StopProjectAsync(ProjectId projectId);
    Task DeleteProjectAsync(ProjectId projectId);
}