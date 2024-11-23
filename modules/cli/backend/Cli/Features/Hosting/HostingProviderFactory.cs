using Cli.Features.Hosting.Providers.Flyio;
using Cli.Services;
using Microsoft.Extensions.Configuration;

namespace Cli.Features.Hosting;

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