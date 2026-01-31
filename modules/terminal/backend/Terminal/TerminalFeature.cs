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
        // HostingManagerTerminalClient must be transient - each connection needs its own instance
        // The TerminalConnectionManager (singleton) stores them by SignalR connection ID
        services.AddTransient<IHostingManagerTerminalClient, HostingManagerTerminalClient>();
        services.AddSingleton<ITerminalConnectionManager, TerminalConnectionManager>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TerminalCommandExecutedEventHandler>());
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        app.MapHub<TerminalHub>("/terminal-hub");
    }
}
