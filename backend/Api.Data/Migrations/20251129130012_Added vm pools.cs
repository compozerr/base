using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addedvmpools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VMPools",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectType = table.Column<int>(type: "integer", nullable: false),
                    ServerTierId = table.Column<string>(type: "text", nullable: false),
                    InstanceCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMPools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VMPools_Servers_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "api",
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VMPoolItems",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DelegatedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VMPoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMPoolItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VMPoolItems_VMPools_VMPoolId",
                        column: x => x.VMPoolId,
                        principalSchema: "api",
                        principalTable: "VMPools",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VMPoolItems_VMPoolId",
                schema: "api",
                table: "VMPoolItems",
                column: "VMPoolId");

            migrationBuilder.CreateIndex(
                name: "IX_VMPools_ServerId",
                schema: "api",
                table: "VMPools",
                column: "ServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VMPoolItems",
                schema: "api");

            migrationBuilder.DropTable(
                name: "VMPools",
                schema: "api");
        }
    }
}
