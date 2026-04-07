using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillProjectEnvironmentIdOnServicesAndDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill ProjectEnvironmentId on rows that predate the AddEnvironmentIdToDomain
            // migration. Each row is assigned its project's production environment. Preview
            // environments never existed before the environment-scoping change so there is
            // nothing else to resolve against. EnvironmentType.Production = 0.
            migrationBuilder.Sql(
                """
                UPDATE api."ProjectServices" ps
                SET "ProjectEnvironmentId" = pe."Id"
                FROM api."ProjectEnvironments" pe
                WHERE ps."ProjectEnvironmentId" IS NULL
                  AND pe."ProjectId" = ps."ProjectId"
                  AND pe."Type" = 0
                  AND pe."DeletedAtUtc" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE api."Domains" d
                SET "ProjectEnvironmentId" = pe."Id"
                FROM api."ProjectEnvironments" pe
                WHERE d."ProjectEnvironmentId" IS NULL
                  AND pe."ProjectId" = d."ProjectId"
                  AND pe."Type" = 0
                  AND pe."DeletedAtUtc" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill; no reversal possible since the original rows had NULL.
        }
    }
}
