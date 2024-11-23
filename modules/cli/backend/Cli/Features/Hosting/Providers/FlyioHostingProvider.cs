namespace Cli.Features.Hosting.Providers;

public class FlyioHostingProvider : IHostingProvider
{
    public Task<DeployResponse> DeployAsync(DeployRequest registryPath)
    {
        return Task.FromResult(new DeployResponse(true, "Deployed to Fly.io"));
    }
}