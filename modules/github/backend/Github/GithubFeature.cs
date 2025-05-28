using Core.Extensions;
using Core.Feature;
using Github.Data;
using Github.Options;
using Github.Repositories;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Octokit.Webhooks;

namespace Github;

public class GithubFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GithubDbContext>(options =>
       {
           options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
           {
               b.MigrationsAssembly(typeof(GithubDbContext).Assembly.FullName);

           });
       });

        services.AddRequiredConfigurationOptions<GithubAppOptions>("Github:GithubApp");

        services.AddSingleton<IGithubJsonWebTokenService, GithubJsonWebTokenService>();
        services.AddScoped<IGithubService, GithubService>();
        services.AddScoped<IGithubUserSettingsRepository, GithubUserSettingsRepository>();
        services.AddDataProtection();
        services.AddHttpClient();

        services.AddSingleton<WebhookEventProcessor, DefaultWebhookEventProcessor>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GithubDbContext>();

        context.Database.Migrate();
    }
}