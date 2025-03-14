using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeploymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deployments_Servers_ServerId",
                schema: "api",
                table: "Deployments");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServerId",
                schema: "api",
                table: "Deployments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "api",
                table: "Deployments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Deployments_Servers_ServerId",
                schema: "api",
                table: "Deployments",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deployments_Servers_ServerId",
                schema: "api",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "api",
                table: "Deployments");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServerId",
                schema: "api",
                table: "Deployments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Deployments_Servers_ServerId",
                schema: "api",
                table: "Deployments",
                column: "ServerId",
                principalSchema: "api",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
