using Carter;
using Core.Extensions;
using Core.Feature;
using Core.Helpers;
using Core.Helpers.Env;
using Core.MediatR;
using Core.Options;
using Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Core;

public class CoreFeature : IFeature
{
    public void ConfigureBuilder(WebApplicationBuilder builder)
    {
        if (builder.Environment.IsProduction())
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5000);
            });
        }

        builder.Configuration.AddEnvFile(".env");

        builder.Host.UseSerilog();
    }

    public void ConfigureApp(WebApplication app)
    {
        app.MapGroup("v1").MapCarter();
        app.UseCors(AppConstants.CorsPolicy);

        // Add middleware to log API requests
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            // Customize the logging level based on status code
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null || httpContext.Response.StatusCode > 499)
                    return LogEventLevel.Error;
                if (httpContext.Response.StatusCode > 399)
                    return LogEventLevel.Warning;

                return LogEventLevel.Information;
            };

            // Attach additional properties to the request log
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            };
        });

        // Use the extracted global exception handler
        app.UseGlobalExceptionHandler();
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        AddWebApiConfig(services, configuration);

        services.UseMediatR();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<ILinks, Links>()
                .AddHttpContextAccessor();

        services.AddTransient<IMyServerUrlAccessor, MyServerUrlAccessor>();

        services.AddRequiredConfigurationOptions<JwtOptions>("Jwt");

        services.AddSingleton<IJsonWebTokenService, JsonWebTokenService>();

        services.AddSingleton<IFrontendLocation, FrontendLocation>();
    }

    private static void AddWebApiConfig(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCarter();

        services.AddCors(options =>
        {
            options.AddPolicy(name: AppConstants.CorsPolicy,
                builder =>
                {
                    builder.WithOrigins(configuration["Cors:AllowedOrigins"]!.Split(";"))
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
        });

        services.AddEndpointsApiExplorer();
    }
}
