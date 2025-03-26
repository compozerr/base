using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Commitdetailsadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "BuildDuration",
                schema: "api",
                table: "Deployments",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommitAuthor",
                schema: "api",
                table: "Deployments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommitBranch",
                schema: "api",
                table: "Deployments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommitMessage",
                schema: "api",
                table: "Deployments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildDuration",
                schema: "api",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "CommitAuthor",
                schema: "api",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "CommitBranch",
                schema: "api",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "CommitMessage",
                schema: "api",
                table: "Deployments");
        }
    }
}
