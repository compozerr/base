using System.Text.RegularExpressions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Abstractions;
using Auth.Repositories;
using Jobs;
using Microsoft.EntityFrameworkCore;
using Organizations.Data;
using Organizations.Data.Repositories;
using Organizations.Models;
using Serilog;

namespace Api.Jobs;

public sealed partial class MigrateExistingUsersToPersonalOrgsJob(
    IUserRepository userRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IProjectRepository projectRepository,
    ApiDbContext apiDbContext) : JobBase<MigrateExistingUsersToPersonalOrgsJob>
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<MigrateExistingUsersToPersonalOrgsJob>();

    public override async Task ExecuteAsync()
    {
        Logger.Information("Starting migration of existing users to personal organizations");

        var allUsers = await userRepository.GetAllAsync();
        var migratedCount = 0;
        var skippedCount = 0;

        foreach (var user in allUsers)
        {
            var existingOrg = await organizationRepository.GetPersonalOrgForUserAsync(user.Id);
            if (existingOrg is not null)
            {
                // Already has personal org - just ensure projects are assigned
                await AssignProjectsToOrgAsync(user.Id, existingOrg.Id);
                skippedCount++;
                continue;
            }

            var slug = GenerateSlug(user.Name, user.Id);

            var org = new Organization
            {
                Name = user.Name,
                Slug = slug,
                IsPersonal = true,
                OwnerUserId = user.Id,
                AvatarUrl = user.AvatarUrl
            };

            await organizationRepository.AddAsync(org);

            var member = new OrganizationMember
            {
                OrganizationId = org.Id,
                UserId = user.Id,
                Role = OrganizationRole.Admin
            };

            await memberRepository.AddAsync(member);

            await AssignProjectsToOrgAsync(user.Id, org.Id);

            migratedCount++;
        }

        Logger.Information(
            "Migration complete. Migrated: {MigratedCount}, Skipped (already had org): {SkippedCount}",
            migratedCount, skippedCount);
    }

    private async Task AssignProjectsToOrgAsync(UserId userId, OrganizationId orgId)
    {
        var projects = await apiDbContext.Projects
            .Where(p => p.UserId == userId && p.OrganizationId == null)
            .ToListAsync();

        foreach (var project in projects)
        {
            project.OrganizationId = orgId;
        }

        if (projects.Count > 0)
        {
            await apiDbContext.SaveChangesAsync();
            Logger.Information("Assigned {Count} projects to org {OrgId} for user {UserId}",
                projects.Count, orgId, userId);
        }
    }

    private static string GenerateSlug(string name, UserId userId)
    {
        var baseSlug = SlugRegex().Replace(name.ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "user";

        var shortId = userId.Value.ToString("N")[..8];
        return $"{baseSlug}-{shortId}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
