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

    void IFeature.AfterAllMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrganizationsDbContext>();

        // Create a personal organization for every existing user that doesn't have one
        context.Database.ExecuteSqlRaw(@"
            INSERT INTO organizations.""Organizations"" (""Id"", ""Name"", ""Slug"", ""IsPersonal"", ""OwnerUserId"", ""CreatedAtUtc"", ""AvatarUrl"")
            SELECT
                gen_random_uuid(),
                u.""Name"",
                LOWER(REGEXP_REPLACE(u.""Name"", '[^a-zA-Z0-9]+', '-', 'g')) || '-' || LEFT(REPLACE(u.""Id""::text, '-', ''), 8),
                true,
                u.""Id"",
                NOW(),
                u.""AvatarUrl""
            FROM auth.""Users"" u
            WHERE u.""DeletedAtUtc"" IS NULL
            AND NOT EXISTS (
                SELECT 1 FROM organizations.""Organizations"" o
                WHERE o.""OwnerUserId"" = u.""Id"" AND o.""IsPersonal"" = true
            );
        ");

        // Make each user an admin of their personal organization
        context.Database.ExecuteSqlRaw(@"
            INSERT INTO organizations.""OrganizationMembers"" (""Id"", ""OrganizationId"", ""UserId"", ""Role"", ""CreatedAtUtc"")
            SELECT
                gen_random_uuid(),
                o.""Id"",
                o.""OwnerUserId"",
                0,
                NOW()
            FROM organizations.""Organizations"" o
            WHERE o.""IsPersonal"" = true
            AND NOT EXISTS (
                SELECT 1 FROM organizations.""OrganizationMembers"" m
                WHERE m.""OrganizationId"" = o.""Id"" AND m.""UserId"" = o.""OwnerUserId""
            );
        ");

        // Assign all existing projects to their owner's personal organization
        context.Database.ExecuteSqlRaw(@"
            UPDATE api.""Projects"" p
            SET ""OrganizationId"" = o.""Id""
            FROM organizations.""Organizations"" o
            WHERE o.""OwnerUserId"" = p.""UserId""
            AND o.""IsPersonal"" = true
            AND p.""OrganizationId"" IS NULL;
        ");
    }
}
