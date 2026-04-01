using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Octokit.Webhooks.Events;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class AddPullRequestWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PullRequestWebhookEvents",
                schema: "github",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Event = table.Column<PullRequestEvent>(type: "jsonb", nullable: false),
                    HandledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErroredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestWebhookEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PullRequestWebhookEvents",
                schema: "github");
        }
    }
}
