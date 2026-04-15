using Api.Abstractions;
using Api.Data;
using Auth.Abstractions;
using Auth.Services;
using Core.Feature;
using Microsoft.EntityFrameworkCore;
using Organizations.Services;
using Serilog;

namespace Api.Features.DevSeed;

/// <summary>
/// Seeds the database with mock data for development purposes.
/// Only active in the Development environment.
/// Exposes:
///   POST /v1/dev/seed   - Seeds data for the authenticated user (if they have no projects)
///   DELETE /v1/dev/seed  - Removes all seed data for the authenticated user
/// </summary>
public class DevSeedFeature : IFeature
{
    void IFeature.ConfigureApp(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        // Seed infrastructure data (location + server) at startup so CLI commands work
        DevSeeder.EnsureInfrastructureAsync(app.Services).GetAwaiter().GetResult();

        var devGroup = app.MapGroup("/v1/dev").RequireAuthorization();

        devGroup.MapPost("/seed", async (ApiDbContext apiDb, ICurrentUserAccessor currentUserAccessor, IOrganizationContextAccessor orgContextAccessor) =>
        {
            var userId = currentUserAccessor.CurrentUserId;
            if (userId is null)
                return Results.Unauthorized();

            var orgId = await orgContextAccessor.GetCurrentOrganizationIdOrDefaultAsync();

            var userHasProjects = await apiDb.Projects.AnyAsync(p => p.UserId == userId);
            if (userHasProjects)
                return Results.Ok(new { message = "Already seeded", seeded = false });

            var result = await DevSeeder.SeedForUser(apiDb, userId, orgId);
            return Results.Ok(result);
        });

        devGroup.MapDelete("/seed", async (ApiDbContext apiDb, ICurrentUserAccessor currentUserAccessor) =>
        {
            var userId = currentUserAccessor.CurrentUserId;
            if (userId is null)
                return Results.Unauthorized();

            var projects = await apiDb.Projects.Where(p => p.UserId == userId).Select(p => p.Id).ToListAsync();
            if (projects.Count == 0)
                return Results.Ok(new { message = "Nothing to delete" });

            // Delete in dependency order
            apiDb.ProjectUsages.RemoveRange(apiDb.ProjectUsages.Where(x => projects.Contains(x.ProjectId)));
            var envIds = await apiDb.ProjectEnvironments.Where(x => projects.Contains(x.ProjectId)).Select(x => x.Id).ToListAsync();
            apiDb.ProjectEnvironmentVariables.RemoveRange(apiDb.ProjectEnvironmentVariables.Where(x => x.ProjectEnvironmentId != null && envIds.Contains(x.ProjectEnvironmentId)));
            apiDb.ProjectEnvironments.RemoveRange(apiDb.ProjectEnvironments.Where(x => projects.Contains(x.ProjectId)));
            apiDb.Deployments.RemoveRange(apiDb.Deployments.Where(x => projects.Contains(x.ProjectId)));
            apiDb.ProjectServices.RemoveRange(apiDb.ProjectServices.Where(x => projects.Contains(x.ProjectId)));
            apiDb.Domains.RemoveRange(apiDb.Domains.Where(x => projects.Contains(x.ProjectId)));
            apiDb.Projects.RemoveRange(apiDb.Projects.Where(x => projects.Contains(x.Id)));

            await apiDb.SaveChangesAsync();
            Log.Information("[DevSeed] Deleted {Count} projects for user {UserId}", projects.Count, userId);
            return Results.Ok(new { message = "Deleted", projectsDeleted = projects.Count });
        });

        Log.Information("[DevSeed] Development seed endpoints registered at POST/DELETE /v1/dev/seed");
    }
}
