using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addedsoftdelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Servers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Secrets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "ProjectUsages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "ProjectEnvironmentVariables",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "ProjectEnvironments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Modules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Domains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Deployments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "ProjectUsages");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "ProjectEnvironmentVariables");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Domains");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                schema: "api",
                table: "Deployments");
        }
    }
}
