using Core.Feature;
using Microsoft.AspNetCore.Builder;

namespace Cli.Auth;

public class CliAuthFeature : IFeature
{
    void IFeature.ConfigureApp(WebApplication app)
    {
        app.MapHub<CliAuthHub>("/cli-auth-hub");
    }
}