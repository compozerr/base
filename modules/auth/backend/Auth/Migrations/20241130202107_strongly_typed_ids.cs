using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Migrations
{
    /// <inheritdoc />
    public partial class strongly_typed_ids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                schema: "auth",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Role",
                schema: "auth",
                table: "Role");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "auth",
                newName: "Users",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "Role",
                schema: "auth",
                newName: "Roles",
                newSchema: "auth");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                schema: "auth",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                schema: "auth",
                table: "Roles",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                schema: "auth",
                table: "Roles");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "auth",
                newName: "User",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "auth",
                newName: "Role",
                newSchema: "auth");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                schema: "auth",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Role",
                schema: "auth",
                table: "Role",
                column: "Id");
        }
    }
}
