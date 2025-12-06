using Core.Feature;
using Hangfire;
using Hangfire.Dashboard;
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
            .UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(con.GetConnectionString("DefaultConnection"));
            },
                new PostgreSqlStorageOptions
                {
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    DistributedLockTimeout = TimeSpan.FromMinutes(10),
                    TransactionSynchronisationTimeout = TimeSpan.FromMinutes(1),
                    SchemaName = "hangfire"
                }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
            options.ServerTimeout = TimeSpan.FromMinutes(4);
            options.ShutdownTimeout = TimeSpan.FromSeconds(20);

            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new OnlyRolesAuthorizationFilter("admin")]
        });
    }
}

public class OnlyRolesAuthorizationFilter(params string[] roles) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return roles.Any(httpContext.User.IsInRole);
    }
}