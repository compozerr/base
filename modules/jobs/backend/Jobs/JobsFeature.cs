using Core.Feature;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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
        // Route the dashboard through endpoint routing + the existing "admin"
        // authorization policy (see AuthFeature). This ensures UseAuthentication
        // / UseAuthorization have populated HttpContext.User with role claims
        // from the cookie before the policy is evaluated. UseHangfireDashboard
        // runs as plain middleware and — depending on feature registration
        // order — can execute before UseAuthentication, in which case its own
        // IDashboardAuthorizationFilter always sees an unauthenticated user.
        //
        // Authorization is cleared because endpoint-routing auth already
        // enforces the policy; the default LocalRequestsOnlyAuthorizationFilter
        // would otherwise reject remote requests regardless of role.
        app.MapHangfireDashboardWithAuthorizationPolicy(
            authorizationPolicyName: "admin",
            pattern: "/hangfire",
            options: new DashboardOptions
            {
                Authorization = Array.Empty<IDashboardAuthorizationFilter>()
            });
    }
}
