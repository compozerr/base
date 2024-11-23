namespace Cli.Features.Hosting;

public interface IHostingProvider
{
    public Task<DeployResponse> DeployAsync(DeployRequest registryPath);
}