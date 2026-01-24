using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addedterminal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop index only if it exists (handles cases where table was created without this index)
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS api.""IX_TerminalSessions_UserId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TerminalSessions_UserId",
                schema: "api",
                table: "TerminalSessions",
                column: "UserId");
        }
    }
}
