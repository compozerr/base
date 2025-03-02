using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectEnvironments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectEnvironment",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Branches = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEnvironment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEnvironment_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "api",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEnvironmentVariable",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectEnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEnvironmentVariable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEnvironmentVariable_ProjectEnvironment_ProjectEnviro~",
                        column: x => x.ProjectEnvironmentId,
                        principalSchema: "api",
                        principalTable: "ProjectEnvironment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEnvironment_ProjectId",
                schema: "api",
                table: "ProjectEnvironment",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEnvironmentVariable_ProjectEnvironmentId",
                schema: "api",
                table: "ProjectEnvironmentVariable",
                column: "ProjectEnvironmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectEnvironmentVariable",
                schema: "api");

            migrationBuilder.DropTable(
                name: "ProjectEnvironment",
                schema: "api");
        }
    }
}
