using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Organizations.Data;

#nullable disable

namespace Organizations.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrganizationsDbContext))]
    [Migration("20260410120000_AddLastEmailSentAtUtcToInvitation")]
    public partial class AddLastEmailSentAtUtcToInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastEmailSentAtUtc",
                schema: "organizations",
                table: "OrganizationInvitations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastEmailSentAtUtc",
                schema: "organizations",
                table: "OrganizationInvitations");
        }
    }
}
