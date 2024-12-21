using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class RenamedSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelectedOrganization",
                schema: "github",
                table: "GithubUserSettings",
                newName: "SelectedOrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelectedOrganizationId",
                schema: "github",
                table: "GithubUserSettings",
                newName: "SelectedOrganization");
        }
    }
}
