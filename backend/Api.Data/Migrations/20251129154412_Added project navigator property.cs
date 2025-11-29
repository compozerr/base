using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addedprojectnavigatorproperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VMPoolItems_ProjectId",
                schema: "api",
                table: "VMPoolItems",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_VMPoolItems_Projects_ProjectId",
                schema: "api",
                table: "VMPoolItems",
                column: "ProjectId",
                principalSchema: "api",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VMPoolItems_Projects_ProjectId",
                schema: "api",
                table: "VMPoolItems");

            migrationBuilder.DropIndex(
                name: "IX_VMPoolItems_ProjectId",
                schema: "api",
                table: "VMPoolItems");
        }
    }
}
