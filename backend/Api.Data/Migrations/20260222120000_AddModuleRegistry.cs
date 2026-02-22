using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleRegistryEntries",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Organization = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RepoUrl = table.Column<string>(type: "text", nullable: false),
                    LatestVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    InstallCount = table.Column<int>(type: "integer", nullable: false),
                    Compatibility = table.Column<string>(type: "jsonb", nullable: true),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleRegistryEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleVersionEntries",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleRegistryEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CommitHash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleVersionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleVersionEntries_ModuleRegistryEntries_ModuleRegistryEn~",
                        column: x => x.ModuleRegistryEntryId,
                        principalSchema: "api",
                        principalTable: "ModuleRegistryEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRegistryEntries_Organization_Name",
                schema: "api",
                table: "ModuleRegistryEntries",
                columns: new[] { "Organization", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleVersionEntries_ModuleRegistryEntryId",
                schema: "api",
                table: "ModuleVersionEntries",
                column: "ModuleRegistryEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleVersionEntries",
                schema: "api");

            migrationBuilder.DropTable(
                name: "ModuleRegistryEntries",
                schema: "api");
        }
    }
}
