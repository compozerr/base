using Api.Endpoints.Projects.ProjectEnvironment;
using Api.Options;
using Api.Services;
using Core.Extensions;
using Core.Feature;
using DnsClient;
using Serilog.Events;
using Serilog.Sinks.Humio;

namespace Api;

public class ApiFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IDefaultEnvironmentVariablesAppender, DefaultEnvironmentVariablesAppender>();

        services.AddRequiredConfigurationOptions<EncryptionOptions>("Encryption");

        services.AddSingleton<ILookupClient>(sp =>
        {
            return new LookupClient(new LookupClientOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
            });
        });
    }
}