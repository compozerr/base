using Api.Abstractions;
using Api.Data;

namespace Api.Hosting.Services;

public interface IHostingApi
{
    Task DeployAsync(Deployment deployment);
    Task<ServerUsage?> GetServerUsageAsync();
    Task HealthCheckAsync();
    Task UpdateDomainsForProjectAsync(ProjectId projectId);
}