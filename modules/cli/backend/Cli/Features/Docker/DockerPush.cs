using Carter;
using Cli.Features.GoogleCloud;
using Cli.Features.Hosting;
using Cli.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Cli.Features.Docker;

public class DockerPush : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/docker/push", async (
            HttpContext context,
            [FromHeader(Name = "x-api-key")] string apiKey,
            [FromHeader(Name = "x-app-name")] string appName,
            IApiKeyService apiKeyService,
            IConfiguration configuration,
            IProcessService processService,
            GoogleAuthService googleAuthService,
            IHostingProviderFactory hostingProvider) =>
        {
            try
            {
                googleAuthService.SetupGoogleCredentials();
                var config = GetGoogleCloudConfiguration(configuration);
                var registryPath = $"{config.Region}-docker.pkg.dev/{config.ProjectId}/{config.RepositoryName}/{appName}";

                if (!await AuthenticateGoogleCloudAsync(processService))
                    return Results.Problem("Failed to authenticate Google Cloud");

                if (!await AuthenticateDockerRegistryAsync(processService, config.Region))
                    return Results.Problem("Failed to authenticate Docker registry");

                if (!await LoadDockerImageAsync(processService, context.Request.Body))
                    return Results.Problem("Failed to load Docker image");

                if (!await TagDockerImageAsync(processService, appName, registryPath))
                    return Results.Problem("Failed to tag Docker image");

                if (!await PushDockerImageAsync(processService, registryPath))
                    return Results.Problem("Failed to push Docker image");

                var hostingProviderInstance = await hostingProvider.GetProviderAsync();

                var deployResponse = await hostingProviderInstance.DeployAsync(new(registryPath));

                if (!deployResponse.Success)
                {
                    return Results.Problem("Failed to deploy image with message" + deployResponse.Message);
                }

                return Results.Ok("Successfully pushed image, and deployed with output " + deployResponse.Message);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Push error");
                return Results.Problem($"Push error: {ex.Message}");
            }
        });
    }
    public record GoogleCloudConfiguration(string Region, string ProjectId, string RepositoryName);

    public static GoogleCloudConfiguration GetGoogleCloudConfiguration(IConfiguration configuration)
    {
        return new GoogleCloudConfiguration(
            configuration["GoogleCloud:Region"] ?? throw new InvalidOperationException("GoogleCloud:Region not found"),
            configuration["GoogleCloud:ProjectId"] ?? throw new InvalidOperationException("GoogleCloud:ProjectId not found"),
            configuration["GoogleCloud:RepositoryName"] ?? throw new InvalidOperationException("GoogleCloud:RepositoryName not found")
        );
    }

    private async static Task<bool> AuthenticateDockerRegistryAsync(IProcessService processService, string region)
    {
        Log.Logger.Information("Authenticating Docker registry");
        var response = await processService.RunProcessAsync($"gcloud auth configure-docker {region}-docker.pkg.dev");
        if (!response.Success)
        {
            Log.Logger.Error($"Failed to authenticate Docker registry {response.Output}");
            return false;
        }

        Log.Logger.Information("Docker registry authenticated");
        return true;
    }

    private async static Task<bool> AuthenticateGoogleCloudAsync(IProcessService processService)
    {
        Log.Logger.Information("Service account activating");
        var response = await processService.RunProcessAsync($"gcloud auth activate-service-account --key-file {Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")}");

        if (!response.Success)
        {
            Log.Logger.Error($"Service account activation failed {response.Output}");
            return false;
        }
        Log.Logger.Information("Service account activated");

        return true;
    }

    private async static Task<bool> LoadDockerImageAsync(IProcessService processService, Stream imageStream)
    {
        Log.Logger.Information("Docker image loading");
        var response = await processService.RunProcessAsync("docker load", imageStream);
        if (!response.Success)
        {
            Log.Logger.Error($"Docker image failed to load {response.Output}");
            return false;
        }

        Log.Logger.Information("Docker image loaded");
        return true;
    }

    private async static Task<bool> TagDockerImageAsync(IProcessService processService, string appName, string registryPath)
    {
        Log.Logger.Information("Docker image tagging");
        var response = await processService.RunProcessAsync($"docker tag {appName} {registryPath}");

        if (!response.Success)
        {
            Log.Logger.Error($"Docker image failed to tag {response.Output}");
            return false;
        }

        Log.Logger.Information("Docker image tagged");
        return true;
    }

    private async static Task<bool> PushDockerImageAsync(IProcessService processService, string registryPath)
    {
        Log.Logger.Information("Docker image pushing");
        var response = await processService.RunProcessAsync($"docker push {registryPath}");

        if (!response.Success)
        {
            Log.Logger.Error($"Docker image failed to push {response.Output}");
            return false;
        }

        Log.Logger.Information("Docker image pushed");
        return true;
    }
}