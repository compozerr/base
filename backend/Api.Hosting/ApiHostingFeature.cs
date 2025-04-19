using Api.Hosting.Jobs;
using Api.Hosting.Services;
using Core.Feature;
using Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Cron = Jobs.Cron;

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
        app.AddRecurringJob<UpdateServerUsageJob>(Cron.Hourly());
        // app.AddRecurringJob<UpdateProjectsUsageJob>(Cron.Hourly());
    }
}
