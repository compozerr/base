using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DomainVerificationTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HealthCheckPassed",
                schema: "api",
                table: "Domains",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerificationCheckUtc",
                schema: "api",
                table: "Domains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextVerificationCheckUtc",
                schema: "api",
                table: "Domains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationCheckCount",
                schema: "api",
                table: "Domains",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthCheckPassed",
                schema: "api",
                table: "Domains");

            migrationBuilder.DropColumn(
                name: "LastVerificationCheckUtc",
                schema: "api",
                table: "Domains");

            migrationBuilder.DropColumn(
                name: "NextVerificationCheckUtc",
                schema: "api",
                table: "Domains");

            migrationBuilder.DropColumn(
                name: "VerificationCheckCount",
                schema: "api",
                table: "Domains");
        }
    }
}
