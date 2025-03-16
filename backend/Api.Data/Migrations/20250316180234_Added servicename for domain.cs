using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addedservicenamefordomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Domain_Servers_ServerId",
                schema: "api",
                table: "Domain");

            migrationBuilder.DropIndex(
                name: "IX_Domain_ServerId",
                schema: "api",
                table: "Domain");

            migrationBuilder.DropColumn(
                name: "ServerId",
                schema: "api",
                table: "Domain");

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                schema: "api",
                table: "Domain",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ServerId",
                schema: "api",
                table: "Projects",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Servers_ServerId",
                schema: "api",
                table: "Projects",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Servers_ServerId",
                schema: "api",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ServerId",
                schema: "api",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                schema: "api",
                table: "Domain");

            migrationBuilder.AddColumn<Guid>(
                name: "ServerId",
                schema: "api",
                table: "Domain",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Domain_ServerId",
                schema: "api",
                table: "Domain",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Domain_Servers_ServerId",
                schema: "api",
                table: "Domain",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
