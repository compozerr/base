using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mail.Migrations
{
    /// <inheritdoc />
    public partial class InitialSentEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mail");

            migrationBuilder.CreateTable(
                name: "SentEmails",
                schema: "mail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromAddress = table.Column<string>(type: "text", nullable: false),
                    FromDisplayName = table.Column<string>(type: "text", nullable: true),
                    ToAddress = table.Column<string>(type: "text", nullable: false),
                    ToDisplayName = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    TemplateName = table.Column<string>(type: "text", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentEmails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SentEmails_SentAtUtc",
                schema: "mail",
                table: "SentEmails",
                column: "SentAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SentEmails",
                schema: "mail");
        }
    }
}
