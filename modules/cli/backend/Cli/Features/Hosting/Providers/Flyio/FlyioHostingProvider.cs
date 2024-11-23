using System.Text.Json;

namespace Cli.Features.Hosting.Providers.Flyio;

public class FlyioHostingProvider(IFlyioNameGenerator flyioNameGenerator) : IHostingProvider
{
    private const string FlyioJsonFileName = "flyio.generated.json";

    public async Task<DeployResponse> DeployAsync(DeployRequest deployRequest)
    {
        await UpdateJsonFileAsync(deployRequest.AppName, deployRequest.RegistryPath, deployRequest.Platform);
        return new DeployResponse(true, "Deployed to Fly.io");
    }

    private async Task UpdateJsonFileAsync(string appName, string imageTag, Platform platform)
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