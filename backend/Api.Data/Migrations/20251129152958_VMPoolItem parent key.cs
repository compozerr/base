using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class VMPoolItemparentkey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VMPoolItems_VMPools_VMPoolId",
                schema: "api",
                table: "VMPoolItems");

            // Delete orphaned VMPoolItems (items without a valid VMPoolId)
            migrationBuilder.Sql(
                @"DELETE FROM api.""VMPoolItems""
                  WHERE ""VMPoolId"" IS NULL
                     OR ""VMPoolId"" NOT IN (SELECT ""Id"" FROM api.""VMPools"")");

            migrationBuilder.AlterColumn<Guid>(
                name: "VMPoolId",
                schema: "api",
                table: "VMPoolItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VMPoolItems_VMPools_VMPoolId",
                schema: "api",
                table: "VMPoolItems",
                column: "VMPoolId",
                principalSchema: "api",
                principalTable: "VMPools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VMPoolItems_VMPools_VMPoolId",
                schema: "api",
                table: "VMPoolItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "VMPoolId",
                schema: "api",
                table: "VMPoolItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_VMPoolItems_VMPools_VMPoolId",
                schema: "api",
                table: "VMPoolItems",
                column: "VMPoolId",
                principalSchema: "api",
                principalTable: "VMPools",
                principalColumn: "Id");
        }
    }
}
