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
    Task StartProjectAsync(ProjectId projectId, string? environmentName = null);
    Task StopProjectAsync(ProjectId projectId);
    Task DeleteProjectAsync(ProjectId projectId);
    Task DeletePreviewEnvironmentAsync(ProjectId projectId, string environmentName);
    Task<HealthCheckResponse> HealthCheckProjectAsync(ProjectId projectId);
    Task<CommandExecutionResult> ExecuteCommandAsync(ProjectId projectId, string command, string? environmentName = null);
    Task<string> DebugTerminalAsync(ProjectId projectId);
    Task<EnvironmentHealthResponse?> GetEnvironmentHealthAsync(ProjectId projectId, ProjectEnvironmentId environmentId);
}

public sealed record CommandExecutionResult(int ExitCode, string Stdout, string Stderr);