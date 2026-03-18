using Api.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints.Admin;

public static class RunMigrationRoute
{
    public static void MapAdminRoutes(this WebApplication app)
    {
        app.MapPost("/admin/run-org-migration", () =>
        {
            var jobId = MigrateExistingUsersToPersonalOrgsJob.Enqueue();
            return TypedResults.Ok(new { jobId, message = "Migration job enqueued. Check Hangfire dashboard for progress." });
        })
        .RequireAuthorization("admin")
        .WithTags("Admin");
    }
}
