using System.Text.Json;

namespace Cli.Features.Hosting.Providers.Flyio;

public class FlyioHostingProvider : IHostingProvider
{
    public async Task<DeployResponse> DeployAsync(DeployRequest deployRequest)
    {
        await UpdateJsonFileAsync(deployRequest.appName, deployRequest.RegistryPath);
        return new DeployResponse(true, "Deployed to Fly.io");
    }

    private async Task UpdateJsonFileAsync(string appName, string imageTag)
    {
        var json = await File.ReadAllTextAsync(imageTag);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone();


        var primaryRegion = root.GetProperty("primary_region").GetString();
        var httpService = root.GetProperty("http_service").GetRawText();
        var vm = root.GetProperty("vm").GetRawText();

        var updatedJson = new
        {
            app = appName,
            primary_region = primaryRegion,
            build = new
            {
                image = imageTag
            },
            http_service = httpService,
            vm
        };

        var updatedJsonString = JsonSerializer.Serialize(updatedJson);
        await File.WriteAllTextAsync("flyio.generated.json", updatedJsonString);
    }
}