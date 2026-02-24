using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTcpPortAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TcpPortAllocations",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    InternalPort = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    AllocatedPort = table.Column<int>(type: "integer", nullable: false),
                    VmIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TcpPortAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TcpPortAllocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "api",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TcpPortAllocations_AllocatedPort",
                schema: "api",
                table: "TcpPortAllocations",
                column: "AllocatedPort",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TcpPortAllocations_ProjectId",
                schema: "api",
                table: "TcpPortAllocations",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TcpPortAllocations",
                schema: "api");
        }
    }
}
