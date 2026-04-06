using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentIdToDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectEnvironmentId",
                schema: "api",
                table: "ProjectServices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectEnvironmentId",
                schema: "api",
                table: "Domains",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectServices_ProjectEnvironmentId",
                schema: "api",
                table: "ProjectServices",
                column: "ProjectEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_ProjectEnvironmentId",
                schema: "api",
                table: "Domains",
                column: "ProjectEnvironmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Domains_ProjectEnvironments_ProjectEnvironmentId",
                schema: "api",
                table: "Domains",
                column: "ProjectEnvironmentId",
                principalSchema: "api",
                principalTable: "ProjectEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectServices_ProjectEnvironments_ProjectEnvironmentId",
                schema: "api",
                table: "ProjectServices",
                column: "ProjectEnvironmentId",
                principalSchema: "api",
                principalTable: "ProjectEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Domains_ProjectEnvironments_ProjectEnvironmentId",
                schema: "api",
                table: "Domains");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectServices_ProjectEnvironments_ProjectEnvironmentId",
                schema: "api",
                table: "ProjectServices");

            migrationBuilder.DropIndex(
                name: "IX_ProjectServices_ProjectEnvironmentId",
                schema: "api",
                table: "ProjectServices");

            migrationBuilder.DropIndex(
                name: "IX_Domains_ProjectEnvironmentId",
                schema: "api",
                table: "Domains");

            migrationBuilder.DropColumn(
                name: "ProjectEnvironmentId",
                schema: "api",
                table: "ProjectServices");

            migrationBuilder.DropColumn(
                name: "ProjectEnvironmentId",
                schema: "api",
                table: "Domains");
        }
    }
}
