using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedInstallationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                schema: "github",
                table: "Installations");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                schema: "github",
                table: "Installations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                schema: "github",
                table: "Installations");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                schema: "github",
                table: "Installations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
