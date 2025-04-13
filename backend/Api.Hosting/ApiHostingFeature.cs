using Api.Hosting.Jobs;
using Api.Hosting.Services;
using Core.Feature;
using Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting;

public class ApiHostingFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddTransient<IHostingServerHttpClientFactory, HostingServerHttpClientFactory>();
        services.AddTransient<IHostingApiFactory, HostingApiFactory>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        app.Services
            .GetRequiredService<IBackgroundJobManager>()
            .AddHostingJobs();
    }
}
