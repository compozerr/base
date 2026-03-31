using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreviewEnvironmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "api",
                table: "ProjectEnvironments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PullRequestNumber",
                schema: "api",
                table: "ProjectEnvironments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subdomain",
                schema: "api",
                table: "ProjectEnvironments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                schema: "api",
                table: "ProjectEnvironments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "EnvironmentId",
                schema: "api",
                table: "Deployments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_EnvironmentId",
                schema: "api",
                table: "Deployments",
                column: "EnvironmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deployments_ProjectEnvironments_EnvironmentId",
                schema: "api",
                table: "Deployments",
                column: "EnvironmentId",
                principalSchema: "api",
                principalTable: "ProjectEnvironments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deployments_ProjectEnvironments_EnvironmentId",
                schema: "api",
                table: "Deployments");

            migrationBuilder.DropIndex(
                name: "IX_Deployments_EnvironmentId",
                schema: "api",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "PullRequestNumber",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "Subdomain",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                schema: "api",
                table: "Deployments");
        }
    }
}
