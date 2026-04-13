using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Organizations.Data;
using Organizations.Data.Repositories;
using Organizations.Services;

namespace Organizations;

public class OrganizationsFeature : IFeature
{
    // Organizations migrations depend on Auth tables (Users)
    public int Order => 100;

    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrganizationsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
            {
                b.MigrationsAssembly(typeof(OrganizationsDbContext).Assembly.FullName);
                b.MigrationsHistoryTable("__EFMigrationsHistory", "organizations");
            });
        });

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationInvitationRepository, OrganizationInvitationRepository>();

        services.AddScoped<IOrganizationContextAccessor, OrganizationContextAccessor>();
        services.AddScoped<IOrganizationAuthorizationService, OrganizationAuthorizationService>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrganizationsDbContext>();

        context.Database.Migrate();
    }
}
