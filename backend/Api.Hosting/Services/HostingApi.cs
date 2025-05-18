using System.Net.Http.Json;
using Api.Abstractions;
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
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var deploymentId = deployment.Id;

        var userLogin = await githubService.GetUserLoginAsync(deployment.UserId);
        if (userLogin is not { AccessToken: { } accessToken })
            throw new Exception("User access token is null");

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

        var deploymentStatus = DeploymentStatus.Deploying;
        var timeout = TimeSpan.FromMinutes(30);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            var deployResponse = await HttpClient.PostAsync("/projects/deploy", JsonContent.Create(new
            {
                projectId = projectId.Value.ToString(),
                accessToken,
                repoName,
                commitHash,
                deploymentId = deployment.Id.Value.ToString()
            }), cts.Token);

            if (!deployResponse.IsSuccessStatusCode)
            {
                loggerWithContext.Error("Deployment failed: {response}", await deployResponse.Content.ReadAsStringAsync(cts.Token));
                deploymentStatus = DeploymentStatus.Failed;
            }
        }
        catch (OperationCanceledException)
        {
            loggerWithContext.Error("Deployment timed out after {timeout} minutes", timeout.TotalMinutes);
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

        loggerWithContext.Information("Deployment status: {status}", deploymentStatus);

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
}