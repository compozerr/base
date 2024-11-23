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

        var newTag = await TagAndPushImageToFlyioAsync(deployRequest.AppName, deployRequest.RegistryPath);

        if (string.IsNullOrEmpty(newTag))
        {
            return new DeployResponse(false, "Failed to tag image to Fly.io");
        }

        var hasBeenDeployed = await HasBeenDeployedAndSaveConfigAsync(deployRequest, flyioAccessToken);

        if (hasBeenDeployed)
        {
            if (!await RedeployFlyAppAsync(deployRequest, flyioAccessToken, newTag))
            {
                return new DeployResponse(false, "Failed to redeploy Fly.io app");
            }
        }
        else
        {
            if (!await LaunchFlyAppAsync(deployRequest, flyioAccessToken, newTag))
            {
                return new DeployResponse(false, "Failed to launch Fly.io app");
            }
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

    private async Task<bool> LaunchFlyAppAsync(DeployRequest deployRequest, string accessToken, string newTag)
    {
        Log.Information("Launching Fly.io app");
        await CreateDefaultJsonFileAsync(deployRequest.AppName, deployRequest.RegistryPath, deployRequest.Platform);

        var response = await processService.RunProcessAsync($@"
        fly launch 
            --config {FlyioJsonFileName}
            --json 
            --image {newTag}
            --name {deployRequest.AppName}
            --region fra
            --vm-size shared-cpu-1x
            --org compozerr
            --access-token {accessToken} 
            --yes");

        if (!response.Success)
        {
            Log.ForContext("output", response.Output)
               .Error("Failed to launch Fly.io app");
            return false;
        }

        Log.Information("Fly.io app launched");

        return true;
    }

    private async Task<bool> RedeployFlyAppAsync(DeployRequest deployRequest, string accessToken, string newTag)
    {
        Log.Information("Redeploying Fly.io app");
        var response = await processService.RunProcessAsync($@"
            fly deploy 
                --app {deployRequest.AppName} 
                --config {FlyioJsonFileName}
                --json
                --image {newTag}
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

    private async Task<bool> HasBeenDeployedAndSaveConfigAsync(DeployRequest deployRequest, string accessToken)
    {
        var saveResponse = await processService.RunProcessAsync($@"
            fly config save 
                --app {deployRequest.AppName} 
                --config {FlyioJsonFileName} 
                --json 
                --access-token {accessToken} 
                --yes");

        if (!saveResponse.Success)
        {
            Log.ForContext(nameof(saveResponse), saveResponse).Warning("Fly.io has not been launched yet");
        }

        return saveResponse.Success;
    }

    private async Task CreateDefaultJsonFileAsync(string appName, string imageTag, Platform platform)
    {
        appName = flyioNameGenerator.GenerateName(appName, platform);

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

        var updatedJsonString = JsonSerializer.Serialize(updatedJson);
        await File.WriteAllTextAsync(FlyioJsonFileName, updatedJsonString);
    }
}