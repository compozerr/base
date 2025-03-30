using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Domainsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Domain_Projects_ProjectId",
                schema: "api",
                table: "Domain");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Domain",
                schema: "api",
                table: "Domain");

            migrationBuilder.RenameTable(
                name: "Domain",
                schema: "api",
                newName: "Domains",
                newSchema: "api");

            migrationBuilder.RenameIndex(
                name: "IX_Domain_ProjectId",
                schema: "api",
                table: "Domains",
                newName: "IX_Domains_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Domains",
                schema: "api",
                table: "Domains",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Domains_Projects_ProjectId",
                schema: "api",
                table: "Domains",
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
                name: "FK_Domains_Projects_ProjectId",
                schema: "api",
                table: "Domains");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Domains",
                schema: "api",
                table: "Domains");

            migrationBuilder.RenameTable(
                name: "Domains",
                schema: "api",
                newName: "Domain",
                newSchema: "api");

            migrationBuilder.RenameIndex(
                name: "IX_Domains_ProjectId",
                schema: "api",
                table: "Domain",
                newName: "IX_Domain_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Domain",
                schema: "api",
                table: "Domain",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Domain_Projects_ProjectId",
                schema: "api",
                table: "Domain",
                column: "ProjectId",
                principalSchema: "api",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
