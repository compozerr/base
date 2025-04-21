using Core.Feature;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jobs;

public class JobsFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration con)
    {
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(con.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        app.UseHangfireDashboard();
    }
}