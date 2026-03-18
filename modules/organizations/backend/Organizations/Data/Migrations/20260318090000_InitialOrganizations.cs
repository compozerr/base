using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Organizations.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organizations");

            migrationBuilder.CreateTable(
                name: "Organizations",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPersonal = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organizations",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationInvitations",
                schema: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organizations",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                schema: "organizations",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerUserId",
                schema: "organizations",
                table: "Organizations",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_UserId",
                schema: "organizations",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_UserId",
                schema: "organizations",
                table: "OrganizationMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_Token",
                schema: "organizations",
                table: "OrganizationInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_OrganizationId_Email_Status",
                schema: "organizations",
                table: "OrganizationInvitations",
                columns: new[] { "OrganizationId", "Email", "Status" });

            // Data migration: create a personal organization for every existing user
            // and make them an admin member of it
            migrationBuilder.Sql(@"
                INSERT INTO organizations.""Organizations"" (""Id"", ""Name"", ""Slug"", ""IsPersonal"", ""OwnerUserId"", ""CreatedAtUtc"", ""AvatarUrl"")
                SELECT
                    gen_random_uuid(),
                    u.""Name"",
                    LOWER(REGEXP_REPLACE(u.""Name"", '[^a-zA-Z0-9]+', '-', 'g')) || '-' || LEFT(REPLACE(u.""Id""::text, '-', ''), 8),
                    true,
                    u.""Id"",
                    NOW(),
                    u.""AvatarUrl""
                FROM auth.""Users"" u
                WHERE u.""DeletedAtUtc"" IS NULL
                AND NOT EXISTS (
                    SELECT 1 FROM organizations.""Organizations"" o
                    WHERE o.""OwnerUserId"" = u.""Id"" AND o.""IsPersonal"" = true
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO organizations.""OrganizationMembers"" (""Id"", ""OrganizationId"", ""UserId"", ""Role"", ""CreatedAtUtc"")
                SELECT
                    gen_random_uuid(),
                    o.""Id"",
                    o.""OwnerUserId"",
                    0,  -- Admin role
                    NOW()
                FROM organizations.""Organizations"" o
                WHERE o.""IsPersonal"" = true
                AND NOT EXISTS (
                    SELECT 1 FROM organizations.""OrganizationMembers"" m
                    WHERE m.""OrganizationId"" = o.""Id"" AND m.""UserId"" = o.""OwnerUserId""
                );
            ");

            // Assign all existing projects to their owner's personal organization
            migrationBuilder.Sql(@"
                UPDATE api.""Projects"" p
                SET ""OrganizationId"" = o.""Id""
                FROM organizations.""Organizations"" o
                WHERE o.""OwnerUserId"" = p.""UserId""
                AND o.""IsPersonal"" = true
                AND p.""OrganizationId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationInvitations",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "OrganizationMembers",
                schema: "organizations");

            migrationBuilder.DropTable(
                name: "Organizations",
                schema: "organizations");
        }
    }
}
