using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Github.Migrations
{
    /// <inheritdoc />
    public partial class Addedpushwebhookeventhandledanderroredat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "github",
                table: "PushWebhookEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ErroredAt",
                schema: "github",
                table: "PushWebhookEvents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HandledAt",
                schema: "github",
                table: "PushWebhookEvents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "github",
                table: "PushWebhookEvents");

            migrationBuilder.DropColumn(
                name: "ErroredAt",
                schema: "github",
                table: "PushWebhookEvents");

            migrationBuilder.DropColumn(
                name: "HandledAt",
                schema: "github",
                table: "PushWebhookEvents");
        }
    }
}
