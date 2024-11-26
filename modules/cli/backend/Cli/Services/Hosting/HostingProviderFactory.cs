using Cli.Services.Hosting.Providers.Flyio;
using Microsoft.Extensions.Configuration;

namespace Cli.Services.Hosting;

public interface IHostingProviderFactory
{
    public Task<IHostingProvider> GetProviderAsync();
}

public class HostingProviderFactory(
    IFlyioNameGenerator flyioNameGenerator,
    IProcessService processService,
    IConfiguration configuration) : IHostingProviderFactory
{
    public Task<IHostingProvider> GetProviderAsync()
    {
        return Task.FromResult<IHostingProvider>(new FlyioHostingProvider(
            flyioNameGenerator,
            processService,
            configuration));
    }
}