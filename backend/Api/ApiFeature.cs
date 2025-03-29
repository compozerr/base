using Api.Endpoints.Projects.ProjectEnvironment;
using Api.Options;
using Api.Services;
using Core.Extensions;
using Core.Feature;

namespace Api;

public class ApiFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IDefaultEnvironmentVariablesAppender, DefaultEnvironmentVariablesAppender>();

        services.AddRequiredConfigurationOptions<EncryptionOptions>("Encryption");
    }
}