using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class Renamed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelectedOrganizationId",
                schema: "github",
                table: "GithubUserSettings",
                newName: "SelectedInstallationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelectedInstallationId",
                schema: "github",
                table: "GithubUserSettings",
                newName: "SelectedOrganizationId");
        }
    }
}
