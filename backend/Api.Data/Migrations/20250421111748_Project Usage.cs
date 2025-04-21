using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectUsages",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    VmId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CpuUsagePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CpuCount = table.Column<int>(type: "integer", nullable: false),
                    MemoryUsageGb = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalMemoryGb = table.Column<decimal>(type: "numeric", nullable: false),
                    DiskUsageGb = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDiskGb = table.Column<decimal>(type: "numeric", nullable: false),
                    NetworkInBytesPerSec = table.Column<decimal>(type: "numeric", nullable: false),
                    NetworkOutBytesPerSec = table.Column<decimal>(type: "numeric", nullable: false),
                    DiskReadBytesPerSec = table.Column<decimal>(type: "numeric", nullable: false),
                    DiskWriteBytesPerSec = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectUsages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "api",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUsages_ProjectId",
                schema: "api",
                table: "ProjectUsages",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectUsages",
                schema: "api");
        }
    }
}
