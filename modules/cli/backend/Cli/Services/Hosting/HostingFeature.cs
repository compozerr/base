using Core.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Services.Hosting;

public class HostingFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHostingProviderFactory, HostingProviderFactory>();
    }
}