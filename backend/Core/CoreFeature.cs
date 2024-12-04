using Carter;
using Core.Feature;
using Core.Helpers;
using Core.Helpers.Env;
using Core.MediatR;
using Core.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

    public void ConfigureServices(IServiceCollection services)
    {
        AddWebApiConfig(services);

        services.UseMediatR();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<ILinks, Links>()
                .AddHttpContextAccessor();
    }

    private static void AddWebApiConfig(IServiceCollection services)
    {
        services.AddCarter();

        services.AddCors(options =>
        {
            options.AddPolicy(name: AppConstants.CorsPolicy,
                builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
        });

        services.AddEndpointsApiExplorer();
        
    }
}
