using System.Net.Http.Json;
using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Dtos;
using Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;
using Github.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Api.Hosting.Services;

public sealed class HostingApi(
    ServerId ServerId,
    IHostingServerHttpClientFactory hostingServerHttpClientFactory,
    IGithubService githubService,
    IProjectRepository projectRepository,
    IServiceProvider serviceProvider) : IHostingApi
{
    private HostingServerHttpClient HttpClient { get; set; } = null!;

    internal async Task<HostingApi> InitializeAsync()
    {
        if (HttpClient is { }) return this;

        HttpClient = await hostingServerHttpClientFactory.GetHostingServerHttpClientAsync(ServerId);

        return this;
    }

    public async Task HealthCheckAsync()
    {
        var response = await HttpClient.GetAsync("/");
        var rawString = await response.Content.ReadAsStringAsync();

        Log.ForContext("BaseDomain", HttpClient.BaseDomain)
           .Information("Health check response: {response}", rawString);
    }


    public async Task<ServerUsage?> GetServerUsageAsync()
    {
        var response = await HttpClient.GetAsync("/monitoring/node-usage");
        var serverUsage = await response.Content.ReadFromJsonAsync<ServerUsage>();
        return serverUsage;
    }

    public async Task UpdateDomainsForProjectAsync(ProjectId projectId)
    {
        var response = await HttpClient.PutAsync("/domains", JsonContent.Create(new
        {
            projectId = projectId.Value.ToString()
        }));

        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to update domains for project {projectId}: {response}", projectId, await response.Content.ReadAsStringAsync());
        }
    }

    public async Task DeployAsync(Deployment deployment)
    {
        using var scope = serviceProvider.CreateScope();
        var scopedServiceProvider = scope.ServiceProvider;
        var mediator = scopedServiceProvider.GetRequiredService<IMediator>();
        var deploymentId = deployment.Id;
        var deploymentStatus = DeploymentStatus.Deploying;

        try
        {
            // Get access token - prefer installation token (auto-refreshes, more reliable)
            string accessToken;
            if (deployment.Project?.GithubInstallationId is { } installationId)
            {
                var installationClient = await githubService
                    .GetInstallationClientByInstallationIdAsync(installationId.ToString());

                if (installationClient is null)
                {
                    Log.Warning(
                        "Failed to get installation client for installation {InstallationId}, falling back to OAuth token",
                        installationId);
                    accessToken = await GetValidatedOAuthTokenAsync(deployment.UserId);
                }
                else
                {
                    accessToken = installationClient.InstallationToken;
                    Log.Information(
                        "Using GitHub App installation token for deployment (installation: {InstallationId})",
                        installationId);
                }
            }
            else
            {
                // Fallback to OAuth token for projects without installation ID
                accessToken = await GetValidatedOAuthTokenAsync(deployment.UserId);
            }

            if (deployment?.Project?.RepoUri is null)
            {
                Log.ForContext("BaseDomain", HttpClient.BaseDomain)
                    .ForContext(nameof(deployment.Project), deployment?.Project)
                    .Error("Deployment failed: RepoUri is null");

                throw new Exception("RepoUri is null");
            }

            var repoName = RepoUri.Parse(deployment.Project.RepoUri).RepoName;
            var commitHash = deployment.CommitHash;
            var projectId = deployment.ProjectId;

            var loggerWithContext = Log.ForContext("BaseDomain", HttpClient.BaseDomain)
                .ForContext(nameof(repoName), repoName)
                .ForContext(nameof(commitHash), commitHash)
                .ForContext(nameof(projectId), projectId);

            loggerWithContext.ForContext("OperationId", Guid.NewGuid())
                .ForContext("Timestamp", DateTime.UtcNow)
                .Information("Sending deploy command to server");

            var timeout = TimeSpan.FromMinutes(45);
            var cts = new CancellationTokenSource(timeout);

            HttpClient.SetRequestTimeout(timeout.Add(TimeSpan.FromSeconds(10)));

            try
            {
                var deployResponse = await HttpClient.PostAsync("/projects/deploy", JsonContent.Create(new
                {
                    projectId = projectId.Value.ToString(),
                    accessToken,
                    repoName,
                    commitHash,
                    deploymentId = deployment.Id.Value.ToString(),
                    serverTier = ServerTiers.GetById(deployment.Project.ServerTierId),
                    environmentName = deployment.Environment?.Name,
                    environmentType = deployment.Environment?.Type.ToString().ToLower()
                }), cts.Token);

                if (!deployResponse.IsSuccessStatusCode)
                {
                    loggerWithContext.Error("Deployment failed: {response}",
                        await deployResponse.Content.ReadAsStringAsync(cts.Token));
                    deploymentStatus = DeploymentStatus.Failed;
                }
            }
            catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
            {
                loggerWithContext.ForContext("cancellationReason", ex)
                    .Error("Deployment was cancelled due to something else");
                deploymentStatus = DeploymentStatus.Failed;
            }
            catch (OperationCanceledException ex)
            {
                loggerWithContext.ForContext("cancellationReason", ex)
                    .Error("Deployment timed out after {timeout} minutes", timeout.TotalMinutes);
                deploymentStatus = DeploymentStatus.Failed;
            }
            catch (Exception ex)
            {
                loggerWithContext.Error(ex, "Deployment failed");
                deploymentStatus = DeploymentStatus.Failed;
            }
            finally
            {
                cts.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.ForContext("DeploymentId", deploymentId.Value)
                .Error(ex, "Deployment failed before reaching hosting manager");
            deploymentStatus = DeploymentStatus.Failed;
        }

        Log.ForContext("DeploymentId", deploymentId.Value)
            .Information("Deployment status: {status}", deploymentStatus);

        await mediator.Send(
            new ChangeDeploymentStatusCommand(
                deploymentId,
                deploymentStatus));
    }

    public async Task CancelDeploymentAsync(DeploymentId deploymentId)
    {
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout);

        var response = await HttpClient.PostAsync("/projects/cancel", JsonContent.Create(new
        {
            deploymentId = deploymentId.Value.ToString()
        }), cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to cancel deployment {deploymentId}: {response}",
                   deploymentId, await response.Content.ReadAsStringAsync());
        }
    }

    public async Task<ProjectUsageDto[]?> GetProjectsUsageAsync()
    {
        var response = await HttpClient.GetAsync("/monitoring/vms-usage");
        var projectsUsage = await response.Content.ReadFromJsonAsync<ProjectUsageDto[]>();
        return projectsUsage;
    }

    public async Task StartProjectAsync(ProjectId projectId)
    {
        var response = await HttpClient.PostAsync("/projects/start", JsonContent.Create(new
        {
            projectId = projectId.Value.ToString()
        }));

        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to start project {projectId}: {response}", projectId, await response.Content.ReadAsStringAsync());
        }

        await projectRepository.SetProjectStateAsync(projectId, ProjectState.Starting);
    }

    public async Task StopProjectAsync(ProjectId projectId)
    {
        var response = await HttpClient.PostAsync("/projects/stop", JsonContent.Create(new
        {
            projectId = projectId.Value.ToString()
        }));
        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to stop project {projectId}: {response}", projectId, await response.Content.ReadAsStringAsync());
        }

        await projectRepository.SetProjectStateAsync(projectId, ProjectState.Stopped);
    }

    public async Task DeleteProjectAsync(ProjectId projectId)
    {
        var response = await HttpClient.DeleteAsync($"/projects/{projectId.Value}");
        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to delete project {projectId}: {response}", projectId, await response.Content.ReadAsStringAsync());
        }

        await projectRepository.SetProjectStateAsync(projectId, ProjectState.Deleting);
    }

    public async Task<HealthCheckResponse> HealthCheckProjectAsync(ProjectId projectId)
    {
        var response = await HttpClient.GetAsync($"/projects/health/{projectId.Value}");
        
        Log.ForContext("BaseDomain", HttpClient.BaseDomain)
           .Information("Health check project {projectId}: {response}", projectId, await response.Content.ReadAsStringAsync());

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => new HealthCheckResponse
            {
                Code = HealthCheckCode.NotFound,
                Error = "Project not found"
            },
            System.Net.HttpStatusCode.OK => new HealthCheckResponse
            {
                Code = HealthCheckCode.Healthy
            },
            _ => new HealthCheckResponse
            {
                Code = HealthCheckCode.Unhealthy,
                Error = $"Unexpected status code: {response.StatusCode}"
            }
        };
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(ProjectId projectId, string command)
    {
        var response = await HttpClient.PostAsync("/projects/exec", JsonContent.Create(new
        {
            projectId = projectId.Value.ToString(),
            command
        }));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to execute command in project {projectId}: {response}", projectId, error);
            return new CommandExecutionResult(-1, "", error);
        }

        var result = await response.Content.ReadFromJsonAsync<CommandExecutionResult>();
        return result ?? new CommandExecutionResult(-1, "", "Failed to parse response");
    }

    public async Task<string> DebugTerminalAsync(ProjectId projectId)
    {
        var response = await HttpClient.GetAsync($"/terminal/debug/{projectId.Value}");
        return await response.Content.ReadAsStringAsync();
    }

    public async Task UpdateTcpProxyAsync(ProjectId projectId)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        var projectIds = await dbContext.Projects
            .Where(p => p.ServerId == ServerId)
            .Select(p => p.Id)
            .ToListAsync();

        var allocations = await dbContext.TcpPortAllocations
            .Where(a => projectIds.Contains(a.ProjectId))
            .Select(a => new
            {
                projectId = a.ProjectId.Value.ToString(),
                serviceName = a.ServiceName,
                internalPort = a.InternalPort,
                allocatedPort = a.AllocatedPort,
                vmIp = a.VmIp
            })
            .ToListAsync();

        var response = await HttpClient.PutAsync("/tcp-proxy", JsonContent.Create(new
        {
            allocations
        }));

        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to update TCP proxy for project {projectId}: {response}", projectId, await response.Content.ReadAsStringAsync());
        }
    }

    private async Task<string> GetValidatedOAuthTokenAsync(Auth.Abstractions.UserId userId)
    {
        var userLogin = await githubService.GetUserLoginAsync(userId);
        if (userLogin is not { AccessToken: { } accessToken })
            throw new GitHubTokenRequiredException();

        // Validate token is still valid before attempting deployment
        var isTokenValid = await githubService.ValidateTokenAsync(accessToken);
        if (!isTokenValid)
            throw new GitHubTokenExpiredException();

        return accessToken;
    }
}
