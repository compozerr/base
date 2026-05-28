using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerIdToProjectEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServerId",
                schema: "api",
                table: "ProjectEnvironments",
                type: "uuid",
                nullable: true);

            // Backfill each environment's ServerId from its project's ServerId so that
            // pre-existing envs land on the same server they were already hosted on.
            // Project.ServerId is retained as a deprecated hint (option B) — once the
            // CLI minimum version is bumped, a follow-up migration drops it.
            migrationBuilder.Sql(@"
                UPDATE ""api"".""ProjectEnvironments"" pe
                   SET ""ServerId"" = p.""ServerId""
                  FROM ""api"".""Projects"" p
                 WHERE pe.""ProjectId"" = p.""Id""
                   AND p.""ServerId"" IS NOT NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEnvironments_ServerId",
                schema: "api",
                table: "ProjectEnvironments",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEnvironments_Servers_ServerId",
                schema: "api",
                table: "ProjectEnvironments",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEnvironments_Servers_ServerId",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectEnvironments_ServerId",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropColumn(
                name: "ServerId",
                schema: "api",
                table: "ProjectEnvironments");
        }
    }
}
