using Api.Data.Repositories;
using Api.Data.Services;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Data;

public class ApiDataFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApiDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
            {
                b.MigrationsAssembly(typeof(ApiDbContext).Assembly.FullName);
            });
        });

        services.AddSingleton<IHashService, HashService>();
        services.AddScoped<IProjectEnvironmentRepository, ProjectEnvironmentRepository>();
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IDeploymentRepository, DeploymentRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IDomainRepository, DomainRepository>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        context.Database.Migrate();
    }
}