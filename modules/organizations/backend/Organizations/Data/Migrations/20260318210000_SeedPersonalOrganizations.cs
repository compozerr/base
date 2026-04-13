using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Organizations.Data;

#nullable disable

namespace Organizations.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrganizationsDbContext))]
    [Migration("20260318210000_SeedPersonalOrganizations")]
    public partial class SeedPersonalOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data backfill moved to OrganizationsFeature.AfterAllMigrations to avoid
            // cross-schema dependency on auth/api tables during migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: we don't want to undo user data
        }
    }
}
