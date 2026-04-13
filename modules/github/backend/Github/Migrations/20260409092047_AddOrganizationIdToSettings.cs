using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "github",
                table: "GithubUserSettings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "github",
                table: "GithubUserSettings",
                type: "uuid",
                nullable: true);

            // Backfill OrganizationId from user's personal org.
            // Wrapped in IF EXISTS check because organizations schema may not exist yet
            // on fresh databases (feature migration order is non-deterministic).
            // On fresh DBs this is a no-op (empty table). On existing DBs this migration
            // was already applied with the original backfill.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'organizations' AND table_name = 'Organizations'
                    ) THEN
                        UPDATE github.""GithubUserSettings"" gs
                        SET ""OrganizationId"" = o.""Id""
                        FROM organizations.""Organizations"" o
                        WHERE o.""OwnerUserId"" = gs.""UserId""
                          AND o.""IsPersonal"" = true;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "github",
                table: "GithubUserSettings");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "github",
                table: "GithubUserSettings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
