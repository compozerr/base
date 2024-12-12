using Core.Extensions;
using Core.Feature;
using Github.Options;
using Github.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Github;

public class ExampleFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddRequiredConfigurationOptions<GithubAppOptions>("Github:GithubApp");
        services.AddSingleton<IStateService, StateService>();
        services.AddDataProtection();
    }
}