using Cli.Features.Hosting.Providers;

namespace Cli.Features.Hosting;

public interface IHostingProviderFactory
{
    public Task<IHostingProvider> GetProviderAsync();
}

public class HostingProviderFactory : IHostingProviderFactory
{
    public Task<IHostingProvider> GetProviderAsync()
    {
        return Task.FromResult<IHostingProvider>(new FlyioHostingProvider());
    }
}