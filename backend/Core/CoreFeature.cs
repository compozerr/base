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
    }

    public void ConfigureApp(WebApplication app)
    {
        app.MapGroup("v1").MapCarter();
        app.UseCors(AppConstants.CorsPolicy);
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
