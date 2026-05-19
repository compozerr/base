using System.Net.Http.Json;
using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Dtos;
using Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;
using Github.Services;
using MediatR;
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

    public async Task<EnvironmentHealthResponse?> GetEnvironmentHealthAsync(ProjectId projectId, ProjectEnvironmentId environmentId)
    {
        try
        {
            var response = await HttpClient.GetAsync($"/projects/{projectId.Value}/environments/{environmentId.Value}/health");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<EnvironmentHealthResponse>();
        }
        catch (Exception ex)
        {
            Log.Logger
                .ForContext("projectId", projectId)
                .ForContext("environmentId", environmentId)
                .ForContext("error", ex)
                .Warning("Failed to fetch environment health from hosting");
            return null;
        }
    }

    public async Task UpdateDomainsForProjectAsync(ProjectId projectId)
    {
        var response = await HttpClient.PutJsonAsync("/domains", new
        {
            projectId = projectId.Value.ToString()
        });

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
            if (deployment?.Project?.RepoUri is null)
            {
                Log.ForContext("BaseDomain", HttpClient.BaseDomain)
                    .ForContext(nameof(deployment.Project), deployment?.Project)
                    .Error("Deployment failed: RepoUri is null");

                throw new Exception("RepoUri is null");
            }

            // Resolve the installation that currently owns this repo. We do NOT
            // trust deployment.Project.GithubInstallationId — repos can be
            // transferred between accounts and the user can install/uninstall the
            // app, so we resolve fresh from the repo's current owner via the
            // GitHub App JWT.
            string accessToken;
            try
            {
                var installationClient = await githubService
                    .GetInstallationClientForRepoAsync(deployment.Project.RepoUri);
                accessToken = installationClient.InstallationToken;
                Log.Information(
                    "Using GitHub App installation token for deployment (installation: {InstallationId})",
                    installationClient.InstallationId);
            }
            catch (InstallationNotFoundForRepoException notFoundEx)
            {
                // No installation can see this repo — fall back to the project
                // owner's OAuth token. This covers users who haven't installed the
                // GitHub App on the repo's account yet.
                Log.Warning(notFoundEx,
                    "No GitHub App installation has access to {RepoUri}, falling back to OAuth token",
                    deployment.Project.RepoUri);
                accessToken = await GetValidatedOAuthTokenAsync(deployment.UserId);
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
                var deployResponse = await HttpClient.PostJsonAsync("/projects/deploy", new
                {
                    projectId = projectId.Value.ToString(),
                    accessToken,
                    repoName,
                    commitHash,
                    deploymentId = deployment.Id.Value.ToString(),
                    serverTier = ServerTiers.GetById(
                        deployment.Environment?.ServerTierId
                        ?? deployment.Project.ServerTierId),
                    // environmentId is what the hosting manager forwards to its
                    // service-reporting pipeline. Without it, the manager falls back
                    // to the non-env endpoint and reports services as production.
                    environmentId = deployment.EnvironmentId?.Value.ToString(),
                    environmentName = deployment.Environment?.Name,
                    environmentType = deployment.Environment?.Type.ToString().ToLower(),
                    subdomain = deployment.Environment?.Subdomain
                }, cts.Token);

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

    public async Task<ProjectUsageDto[]?> GetProjectsUsageAsync()
    {
        var response = await HttpClient.GetAsync("/monitoring/vms-usage");
        var projectsUsage = await response.Content.ReadFromJsonAsync<ProjectUsageDto[]>();
        return projectsUsage;
    }

    private const string PreviewSeparator = "--preview--";

    private static string BuildVmIdentifier(ProjectId projectId, string? environmentName)
        => environmentName != null
            ? $"{projectId.Value}{PreviewSeparator}{environmentName}"
            : projectId.Value.ToString();

    public async Task StartProjectAsync(ProjectId projectId, string? environmentName = null)
    {
        var vmIdentifier = BuildVmIdentifier(projectId, environmentName);
        var response = await HttpClient.PostJsonAsync("/projects/start", new
        {
            projectId = vmIdentifier
        });

        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to start project {projectId}: {response}", projectId, await response.Content.ReadAsStringAsync());
        }

        await projectRepository.SetProjectStateAsync(projectId, ProjectState.Starting);
    }

    public async Task StopProjectAsync(ProjectId projectId)
    {
        var response = await HttpClient.PostJsonAsync("/projects/stop", new
        {
            projectId = projectId.Value.ToString()
        });
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

    public async Task DeletePreviewEnvironmentAsync(ProjectId projectId, string environmentName)
    {
        var response = await HttpClient.DeleteAsync($"/projects/{projectId.Value}/preview/{Uri.EscapeDataString(environmentName)}");
        if (!response.IsSuccessStatusCode)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Error("Failed to delete preview environment {EnvironmentName} for project {ProjectId}: {Response}",
                   environmentName, projectId, await response.Content.ReadAsStringAsync());
        }
    }

    public async Task<HealthCheckResponse> HealthCheckProjectAsync(ProjectId projectId)
    {
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.GetAsync($"/projects/health/{projectId.Value}");
        }
        catch (OperationCanceledException)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .Warning("Health check timed out for project {ProjectId}", projectId);
            return new HealthCheckResponse
            {
                Code = HealthCheckCode.Unhealthy,
                Error = "Health check timed out"
            };
        }

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

    public async Task<CommandExecutionResult> ExecuteCommandAsync(ProjectId projectId, string command, string? environmentName = null)
    {
        var vmIdentifier = BuildVmIdentifier(projectId, environmentName);
        var response = await HttpClient.PostJsonAsync("/projects/exec", new
        {
            projectId = vmIdentifier,
            command
        });

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
