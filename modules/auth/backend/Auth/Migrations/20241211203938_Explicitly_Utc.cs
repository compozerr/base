using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Migrations
{
    /// <inheritdoc />
    public partial class Explicitly_Utc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "auth",
                table: "Users",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "auth",
                table: "Users",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "auth",
                table: "UserLogins",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                schema: "auth",
                table: "UserLogins",
                newName: "ExpiresAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "auth",
                table: "UserLogins",
                newName: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "auth",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "auth",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                schema: "auth",
                table: "UserLogins",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ExpiresAtUtc",
                schema: "auth",
                table: "UserLogins",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "auth",
                table: "UserLogins",
                newName: "CreatedAt");
        }
    }
}
