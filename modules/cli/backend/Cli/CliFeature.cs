using Cli.Services;
using Core.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Cli;

public class CliFeature : IFeature
{

    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IApiKeyService, ApiKeyService>();
    }

}