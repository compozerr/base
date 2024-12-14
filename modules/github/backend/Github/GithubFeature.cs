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
using Microsoft.Extensions.Hosting;

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
        services.AddSingleton<IStateService, StateService>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();
        services.AddDataProtection();
        services.AddHttpClient();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GithubDbContext>();

            context.Database.Migrate();
        }
    }
}