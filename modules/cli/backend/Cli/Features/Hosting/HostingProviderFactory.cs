using Cli.Features.Hosting.Providers.Flyio;

namespace Cli.Features.Hosting;

public interface IHostingProviderFactory
{
    public Task<IHostingProvider> GetProviderAsync();
}

public class HostingProviderFactory(IFlyioNameGenerator flyioNameGenerator) : IHostingProviderFactory
{
    public Task<IHostingProvider> GetProviderAsync()
    {
        return Task.FromResult<IHostingProvider>(new FlyioHostingProvider(flyioNameGenerator));
    }
}