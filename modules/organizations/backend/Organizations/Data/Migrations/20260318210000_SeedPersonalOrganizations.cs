using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organizations.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedPersonalOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a personal organization for every existing user that doesn't have one
            migrationBuilder.Sql(@"
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
            migrationBuilder.Sql(@"
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
            migrationBuilder.Sql(@"
                UPDATE api.""Projects"" p
                SET ""OrganizationId"" = o.""Id""
                FROM organizations.""Organizations"" o
                WHERE o.""OwnerUserId"" = p.""UserId""
                AND o.""IsPersonal"" = true
                AND p.""OrganizationId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: we don't want to undo user data
        }
    }
}
