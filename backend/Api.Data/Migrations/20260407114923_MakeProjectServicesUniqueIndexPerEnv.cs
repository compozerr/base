using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeProjectServicesUniqueIndexPerEnv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectServices_ProjectId_Name",
                schema: "api",
                table: "ProjectServices");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectServices_ProjectId_ProjectEnvironmentId_Name",
                schema: "api",
                table: "ProjectServices",
                columns: new[] { "ProjectId", "ProjectEnvironmentId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectServices_ProjectId_ProjectEnvironmentId_Name",
                schema: "api",
                table: "ProjectServices");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectServices_ProjectId_Name",
                schema: "api",
                table: "ProjectServices",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }
    }
}
