using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ServerlocationIdnullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Servers_Locations_LocationId",
                schema: "api",
                table: "Servers");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                schema: "api",
                table: "Servers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Servers_Locations_LocationId",
                schema: "api",
                table: "Servers",
                column: "LocationId",
                principalSchema: "api",
                principalTable: "Locations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Servers_Locations_LocationId",
                schema: "api",
                table: "Servers");

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                schema: "api",
                table: "Servers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Servers_Locations_LocationId",
                schema: "api",
                table: "Servers",
                column: "LocationId",
                principalSchema: "api",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
