using Core.Feature;
using Microsoft.Extensions.DependencyInjection;
using Template.Services;

namespace Template;

public class TemplateFeature : IFeature
{

    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITemplateService, TemplateService>();
    }
}