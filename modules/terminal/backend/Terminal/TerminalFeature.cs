using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Events;
using Terminal.Services;

namespace Terminal;

public class TerminalFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR();
        services.AddHttpClient();
        services.AddScoped<ITerminalSessionService, TerminalSessionService>();
        services.AddScoped<IHostingManagerTerminalClient, HostingManagerTerminalClient>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TerminalCommandExecutedEventHandler>());
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        app.MapHub<TerminalHub>("/terminal-hub");
    }
}
