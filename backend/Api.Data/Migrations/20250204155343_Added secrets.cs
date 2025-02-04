using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addedsecrets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Secret_Servers_ServerId",
                schema: "api",
                table: "Secret");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Secret",
                schema: "api",
                table: "Secret");

            migrationBuilder.RenameTable(
                name: "Secret",
                schema: "api",
                newName: "Secrets",
                newSchema: "api");

            migrationBuilder.RenameIndex(
                name: "IX_Secret_ServerId",
                schema: "api",
                table: "Secrets",
                newName: "IX_Secrets_ServerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Secrets",
                schema: "api",
                table: "Secrets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Secrets_Servers_ServerId",
                schema: "api",
                table: "Secrets",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Secrets_Servers_ServerId",
                schema: "api",
                table: "Secrets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Secrets",
                schema: "api",
                table: "Secrets");

            migrationBuilder.RenameTable(
                name: "Secrets",
                schema: "api",
                newName: "Secret",
                newSchema: "api");

            migrationBuilder.RenameIndex(
                name: "IX_Secrets_ServerId",
                schema: "api",
                table: "Secret",
                newName: "IX_Secret_ServerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Secret",
                schema: "api",
                table: "Secret",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Secret_Servers_ServerId",
                schema: "api",
                table: "Secret",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
