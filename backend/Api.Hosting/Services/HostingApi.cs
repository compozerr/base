using System.Net.Http.Json;
using Api.Abstractions;
using Api.Data;
using Github.Services;
using Serilog;

namespace Api.Hosting.Services;

public sealed class HostingApi(
    ServerId ServerId,
    IHostingServerHttpClientFactory hostingServerHttpClientFactory,
    IGithubService githubService)
{
    private HostingServerHttpClient HttpClient { get; set; } = null!;

    internal async Task<HostingApi> Initialize()
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

    public async Task DeployAsync(Deployment deployment)
    {
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

        try
        {
            var deployResponse = await HttpClient.PostAsync("/projects/deploy", JsonContent.Create(new
            {
                projectId = projectId.Value.ToString(),
                accessToken,
                repoName,
                commitHash,
            }));

            if (!deployResponse.IsSuccessStatusCode)
            {
                Log.ForContext("BaseDomain", HttpClient.BaseDomain)
                   .ForContext(nameof(repoName), repoName)
                   .ForContext(nameof(commitHash), commitHash)
                   .ForContext(nameof(projectId), projectId)
                   .Error("Deployment failed: {response}", await deployResponse.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            Log.ForContext("BaseDomain", HttpClient.BaseDomain)
               .ForContext(nameof(repoName), repoName)
               .ForContext(nameof(commitHash), commitHash)
               .ForContext(nameof(projectId), projectId)
               .Error(ex, "Deployment failed");
        }
    }
}