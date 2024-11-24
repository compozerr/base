using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Migrations
{
    /// <inheritdoc />
    public partial class Users : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_roles_RoleId",
                schema: "auth",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_roles_users_UserId",
                schema: "auth",
                table: "user_roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "auth",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_roles",
                schema: "auth",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_roles",
                schema: "auth",
                table: "user_roles");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "auth",
                newName: "Users",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "auth",
                newName: "Roles",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "user_roles",
                schema: "auth",
                newName: "UserRoles",
                newSchema: "auth");

            migrationBuilder.RenameIndex(
                name: "IX_user_roles_UserId",
                schema: "auth",
                table: "UserRoles",
                newName: "IX_UserRoles_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_user_roles_RoleId",
                schema: "auth",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "auth",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "auth",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "auth",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "auth",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                schema: "auth",
                table: "UserRoles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "auth",
                table: "UserRoles",
                column: "RoleId",
                principalSchema: "auth",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "auth",
                table: "UserRoles",
                column: "UserId",
                principalSchema: "auth",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "auth",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "auth",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                schema: "auth",
                table: "UserRoles");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "auth",
                newName: "users",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "auth",
                newName: "roles",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                schema: "auth",
                newName: "user_roles",
                newSchema: "auth");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_UserId",
                schema: "auth",
                table: "user_roles",
                newName: "IX_user_roles_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                schema: "auth",
                table: "user_roles",
                newName: "IX_user_roles_RoleId");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "auth",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "auth",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "auth",
                table: "roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "auth",
                table: "roles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                schema: "auth",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_roles",
                schema: "auth",
                table: "roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_roles",
                schema: "auth",
                table: "user_roles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_roles_RoleId",
                schema: "auth",
                table: "user_roles",
                column: "RoleId",
                principalSchema: "auth",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_roles_users_UserId",
                schema: "auth",
                table: "user_roles",
                column: "UserId",
                principalSchema: "auth",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
