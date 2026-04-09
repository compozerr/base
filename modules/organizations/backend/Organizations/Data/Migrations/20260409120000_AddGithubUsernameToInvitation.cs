using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Organizations.Data;

#nullable disable

namespace Organizations.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrganizationsDbContext))]
    [Migration("20260409120000_AddGithubUsernameToInvitation")]
    public partial class AddGithubUsernameToInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "organizations",
                table: "OrganizationInvitations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "GithubUsername",
                schema: "organizations",
                table: "OrganizationInvitations",
                type: "character varying(39)",
                maxLength: 39,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_OrganizationId_GithubUsername_Status",
                schema: "organizations",
                table: "OrganizationInvitations",
                columns: new[] { "OrganizationId", "GithubUsername", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationInvitations_OrganizationId_GithubUsername_Status",
                schema: "organizations",
                table: "OrganizationInvitations");

            migrationBuilder.DropColumn(
                name: "GithubUsername",
                schema: "organizations",
                table: "OrganizationInvitations");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "organizations",
                table: "OrganizationInvitations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
