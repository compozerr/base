using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class AddedModulesInstallationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelectedInstallationId",
                schema: "github",
                table: "GithubUserSettings",
                newName: "SelectedProjectsInstallationId");

            migrationBuilder.AddColumn<string>(
                name: "SelectedModulesInstallationId",
                schema: "github",
                table: "GithubUserSettings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedModulesInstallationId",
                schema: "github",
                table: "GithubUserSettings");

            migrationBuilder.RenameColumn(
                name: "SelectedProjectsInstallationId",
                schema: "github",
                table: "GithubUserSettings",
                newName: "SelectedInstallationId");
        }
    }
}
