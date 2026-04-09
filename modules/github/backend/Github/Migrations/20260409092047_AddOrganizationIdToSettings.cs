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

            migrationBuilder.Sql(@"
                UPDATE github.""GithubUserSettings"" gs
                SET ""OrganizationId"" = o.""Id""
                FROM organizations.""Organizations"" o
                WHERE o.""OwnerUserId"" = gs.""UserId""
                  AND o.""IsPersonal"" = true;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM github.""GithubUserSettings""
                        WHERE ""OrganizationId"" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Migration validation failed: some GithubUserSettings rows have NULL OrganizationId after backfill. These users may not have personal organizations.';
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
