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
        var flyioAccessToken = $"'{configuration["FLYIO_ACCESS_TOKEN"]}'";

        if (string.IsNullOrEmpty(flyioAccessToken))
        {
            Log.Error("FLYIO_ACCESS_TOKEN is not set");
            return new DeployResponse(false, "FLYIO_ACCESS_TOKEN is not set");
        }

        Log.ForContext(nameof(deployRequest), deployRequest).Information("Deploying to Fly.io");

        var hasBeenDeployed = await HasBeenDeployedAndSaveConfigAsync(deployRequest, flyioAccessToken);

        if (hasBeenDeployed)
        {
            await RedeployFlyAppAsync(deployRequest, flyioAccessToken);
        }
        else
        {
            await LaunchFlyAppAsync(deployRequest, flyioAccessToken);
        }

        return new DeployResponse(true, "Deployed to Fly.io");
    }

    private async Task LaunchFlyAppAsync(DeployRequest deployRequest, string accessToken)
    {
        Log.Information("Launching Fly.io app");
        await CreateDefaultJsonFileAsync(deployRequest.AppName, deployRequest.RegistryPath, deployRequest.Platform);

        var response = await processService.RunProcessAsync($@"
        fly launch 
            --app {deployRequest.AppName} 
            --config {FlyioJsonFileName}
            --json 
            --image {deployRequest.RegistryPath}
            --access-token {accessToken} 
            --yes");

        if (!response.Success)
        {
            Log.ForContext("output", response.Output)
               .Error("Failed to launch Fly.io app");
            return;
        }

        Log.Information("Fly.io app launched");
    }

    private async Task RedeployFlyAppAsync(DeployRequest deployRequest, string accessToken)
    {
        Log.Information("Redeploying Fly.io app");
        var response = await processService.RunProcessAsync($@"
            fly deploy 
                --app {deployRequest.AppName} 
                --config {FlyioJsonFileName}
                --json
                --image {deployRequest.RegistryPath}
                --access-token {accessToken} 
                --yes");

        if (!response.Success)
        {
            Log.ForContext("output", response.Output)
               .Error("Failed to redeploy Fly.io app");
            return;
        }

        Log.Information("Fly.io app redeployed");
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