using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Auth;

public class CliAuthFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        app.MapHub<CliAuthHub>("/cli-auth-hub");
    }
}