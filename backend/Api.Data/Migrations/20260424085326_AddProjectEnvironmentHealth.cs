using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectEnvironmentHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveHealthFailures",
                schema: "api",
                table: "ProjectEnvironments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HealthStatus",
                schema: "api",
                table: "ProjectEnvironments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastHealthCheckAtUtc",
                schema: "api",
                table: "ProjectEnvironments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveHealthFailures",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "HealthStatus",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "LastHealthCheckAtUtc",
                schema: "api",
                table: "ProjectEnvironments");
        }
    }
}
