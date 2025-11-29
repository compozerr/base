using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Changedvmpooltouselocationidinstead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VMPools_Servers_ServerId",
                schema: "api",
                table: "VMPools");

            migrationBuilder.DropIndex(
                name: "IX_VMPools_ServerId",
                schema: "api",
                table: "VMPools");

            migrationBuilder.RenameColumn(
                name: "ServerId",
                schema: "api",
                table: "VMPools",
                newName: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocationId",
                schema: "api",
                table: "VMPools",
                newName: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_VMPools_ServerId",
                schema: "api",
                table: "VMPools",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_VMPools_Servers_ServerId",
                schema: "api",
                table: "VMPools",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
