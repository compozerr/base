using Cli.Services;
using Cli.Services.GoogleCloud;
using Core.Feature;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cli;

public class CliFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddSingleton<GoogleAuthService>();
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 1024L * 1024L * 2048L; // 2GB
            options.Limits.MinRequestBodyDataRate = new(100, TimeSpan.FromSeconds(10));
        });
        services.AddSingleton<IProcessService, ProcessService>();
    }

    void IFeature.ConfigureBuilder(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
    {
        builder.Configuration.AddUserSecrets<GoogleAuthService>();
    }
}