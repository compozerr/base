using Core.Feature;
using Microsoft.Extensions.DependencyInjection;
using Template.Services;

namespace Template;

public class ExampleFeature : IFeature
{

    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IExampleService, ExampleService>();
    }
}