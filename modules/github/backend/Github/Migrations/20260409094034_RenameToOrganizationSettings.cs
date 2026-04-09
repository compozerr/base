using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class RenameToOrganizationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GithubUserSettings",
                schema: "github");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                schema: "github",
                table: "GithubUserSettings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true,
                oldType: "uuid");

            migrationBuilder.RenameTable(
                name: "GithubUserSettings",
                schema: "github",
                newName: "GithubOrganizationSettings",
                newSchema: "github");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "GithubOrganizationSettings",
                schema: "github",
                newName: "GithubUserSettings",
                newSchema: "github");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                schema: "github",
                table: "GithubUserSettings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldNullable: false,
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "github",
                table: "GithubUserSettings",
                type: "uuid",
                nullable: true);
        }
    }
}
