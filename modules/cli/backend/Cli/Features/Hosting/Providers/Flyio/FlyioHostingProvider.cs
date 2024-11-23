using System.Text.Json;
using Cli.Services;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Cli.Features.Hosting.Providers.Flyio;

public class FlyioHostingProvider(
    IFlyioNameGenerator flyioNameGenerator,
    IProcessService processService,
    IConfiguration configuration) : IHostingProvider
{
    private const string FlyioJsonFileName = "flyio.generated.json";

    public async Task<DeployResponse> DeployAsync(DeployRequest deployRequest)
    {
        //Define the name generated by the flyioNameGenerator
        deployRequest = deployRequest with { AppName = flyioNameGenerator.GenerateName(deployRequest.AppName, deployRequest.Platform) };

        var flyioAccessToken = $"\"{configuration["FLYIO_ACCESS_TOKEN"]}\"";

        if (string.IsNullOrEmpty(flyioAccessToken))
        {
            Log.Error("FLYIO_ACCESS_TOKEN is not set");
            return new DeployResponse(false, "FLYIO_ACCESS_TOKEN is not set");
        }

        Log.ForContext(nameof(deployRequest), deployRequest).Information("Deploying to Fly.io");

        if (!await AuthenticateFlyioWithDockerAsync())
        {
            return new DeployResponse(false, "Failed to authenticate Fly.io with Docker");
        }

        if (!await CreateAppIfNotExistingAsync(deployRequest.AppName, flyioAccessToken))
        {
            return new DeployResponse(false, "Failed to create Fly.io app");
        }

        var newTag = await TagAndPushImageToFlyioAsync(deployRequest.AppName, deployRequest.RegistryPath);

        if (string.IsNullOrEmpty(newTag))
        {
            return new DeployResponse(false, "Failed to tag image to Fly.io");
        }

        if (!await CreateDefaultJsonFileAsync(deployRequest.AppName, newTag))
        {
            return new DeployResponse(false, "Failed to create Fly.io json file");
        }

        if (!await DeployFlyAppAsync(deployRequest, flyioAccessToken, newTag))
        {
            return new DeployResponse(false, "Failed to redeploy Fly.io app");
        }

        return new DeployResponse(true, "Deployed to Fly.io");
    }

    private async Task<bool> AuthenticateFlyioWithDockerAsync()
    {
        var response = await processService.RunProcessAsync("fly auth docker");

        if (!response.Success)
        {
            Log.ForContext("output", response.Output)
               .Error("Failed to authenticate Fly.io with Docker");
            return false;
        }

        Log.Information("Fly.io authenticated with Docker");
        return true;
    }

    private async Task<bool> CreateAppIfNotExistingAsync(string appName, string accessToken)
    {
        var response = await processService.RunProcessAsync($@"
            fly apps create {appName} 
                --org compozerr
                --access-token {accessToken}
                ");

        if (!response.Success)
        {
            if (response.Output.Contains("Name has already been taken"))
            {
                Log.ForContext(nameof(appName), appName).Information("Fly.io app already exists");
            }
            else
            {
                Log.ForContext(nameof(response), response).Error("Failed to create Fly.io app");
                return false;
            }
        }
        else
        {
            Log.ForContext(nameof(appName), appName).Information("Fly.io app created");
        }

        return true;
    }

    private async Task<string?> TagAndPushImageToFlyioAsync(string appName, string imageTag)
    {
        var newTag = $"registry.fly.io/{appName}";

        var response = await processService.RunProcessAsync($@"
            docker tag {imageTag} {newTag}");

        if (!response.Success)
        {
            Log.ForContext("output", response.Output)
               .Error("Failed to tag image to Fly.io");
            return null;
        }

        Log.Information("Image tagged to Fly.io");

        response = await processService.RunProcessAsync($@"
            docker push {newTag}");

        if (!response.Success)
        {
            Log.ForContext("output", response.Output)
               .Error("Failed to push image to Fly.io");
            return null;
        }

        return newTag;
    }

    private async Task<bool> DeployFlyAppAsync(DeployRequest deployRequest, string accessToken, string newTag)
    {
        Log.Information("Redeploying Fly.io app");
        var response = await processService.RunProcessAsync($@"
            fly deploy 
                --config {FlyioJsonFileName}
                --access-token {accessToken} 
                --yes");

        if (!response.Success)
        {
            Log.ForContext("output", response.Output)
               .Error("Failed to redeploy Fly.io app");
            return false;
        }

        Log.Information("Fly.io app redeployed");

        return true;
    }

    private async static Task<bool> CreateDefaultJsonFileAsync(string appName, string imageTag)
    {
        var updatedJson = new
        {
            app = appName,
            primary_region = "fra",
            build = new
            {
                image = imageTag
            },
            http_service = new
            {
                internal_port = 5000,
                force_https = true,
                auto_stop_machines = false,
                auto_start_machines = true,
                min_machines_running = 0,

                processes = new[] { "app" }
            },
            vm = new
            {
                memory = "256mb",
                cpu_kind = "shared",
                cpus = 1
            }
        };

        try
        {
            var updatedJsonString = JsonSerializer.Serialize(updatedJson, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FlyioJsonFileName, updatedJsonString);
        }
        catch (Exception exception)
        {
            Log.ForContext(nameof(exception), exception).Error("Failed to write Fly.io json file");
            return false;
        }

        return true;
    }
}