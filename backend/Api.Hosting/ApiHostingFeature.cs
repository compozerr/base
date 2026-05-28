using Api.Hosting.Jobs;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Api.Hosting.VMPooling.Core;
using Api.Hosting.VMPooling.Jobs;
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
        services.AddSingleton<ISubdomainHashService, SubdomainHashService>();
        services.AddTransient<IHostingServerHttpClientFactory, HostingServerHttpClientFactory>();
        services.AddTransient<IHostingApiFactory, HostingApiFactory>();
        services.AddScoped<IEnvironmentTargetResolver, EnvironmentTargetResolver>();
        services.AddScoped<IVMPoolItemDelegator, VMPoolItemDelegator>();
        services.AddScoped<IVMPoolerFactory, VMPoolerFactory>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        app.AddRecurringJob<UpdateServerUsageJob>(new Cron("0 */6 * * *")); // Every 6 hours
        app.AddRecurringJob<UpdateProjectsUsageJob>(Cron.Hourly());
        app.AddRecurringJob<VMPoolSyncJob>(Cron.Hourly());
        app.AddRecurringJob<DeploymentTimeoutJob>(new Cron("*/5 * * * *"));
        app.AddRecurringJob<ProjectEnvironmentHealthCheckJob>(new Cron("*/5 * * * *")); // every 5 minutes
    }
}
