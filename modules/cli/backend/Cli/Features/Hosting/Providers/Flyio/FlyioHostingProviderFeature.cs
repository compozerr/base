using Core.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Features.Hosting.Providers.Flyio;

public class FlyioHostingProviderFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFlyioNameGenerator, FlyioNameGenerator>();
    }
}