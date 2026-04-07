using Api.Endpoints.Projects.ProjectEnvironment;
using Api.Jobs;
using Api.Options;
using Api.ProjectTypes;
using Api.Services;
using Core.Extensions;
using Core.Feature;
using DnsClient;
using Jobs;
using Microsoft.AspNetCore.Builder;
using Serilog.Events;
using Serilog.Sinks.Humio;
using Cron = Jobs.Cron;

namespace Api;

public class ApiFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IDefaultEnvironmentVariablesAppender, DefaultEnvironmentVariablesAppender>();
        services.AddScoped<IProjectManager, ProjectManager>();

        // Project type configurations
        services.AddSingleton<IProjectTypeConfiguration, CompozerrProjectTypeConfiguration>();
        services.AddSingleton<IProjectTypeConfiguration, DockerComposeProjectTypeConfiguration>();
        services.AddSingleton<IProjectTypeConfiguration, DockerfileProjectTypeConfiguration>();
        services.AddSingleton<IProjectTypeConfiguration, N8nProjectTypeConfiguration>();
        services.AddSingleton<IProjectTypeConfigurationFactory, ProjectTypeConfigurationFactory>();

        services.AddMemoryCache();

        services.AddRequiredConfigurationOptions<EncryptionOptions>("Encryption");

        services.AddSingleton<ILookupClient>(sp =>
        {
            return new LookupClient(new LookupClientOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
            });
        });
        
        services.ConfigureHttpJsonOptions(options =>
        {
            Core.Helpers.Json.ConfigureOptions(options.SerializerOptions);
        });

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.HumioSink(new HumioSinkConfiguration
            {
                IngestToken = configuration["HUMIOINGESTTOKEN"],
                Tags = new Dictionary<string, string>
                {
                    {"system", "compozerr"},
                    {"platform", "web"},
                    {"environment", configuration["ASPNETCORE_ENVIRONMENT"] == "Production" ? "prod" : "dev"}
                },
                Url = "https://cloud.community.humio.com",
            })
            .CreateLogger();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        // Run every 5 minutes to check domains due for verification
        app.AddRecurringJob<DomainVerificationJob>(new Cron("*/5 * * * *"));

        // Run hourly to detect repositories that have been renamed or
        // transferred on GitHub and auto-update the stored repo URL.
        app.AddRecurringJob<RepositoryCheckJob>(Cron.Hourly());
    }
}