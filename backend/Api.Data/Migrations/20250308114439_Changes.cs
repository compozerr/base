using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEnvironment_Projects_ProjectId",
                schema: "api",
                table: "ProjectEnvironment");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEnvironmentVariable_ProjectEnvironment_ProjectEnviro~",
                schema: "api",
                table: "ProjectEnvironmentVariable");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectEnvironmentVariable",
                schema: "api",
                table: "ProjectEnvironmentVariable");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectEnvironment",
                schema: "api",
                table: "ProjectEnvironment");

            migrationBuilder.RenameTable(
                name: "ProjectEnvironmentVariable",
                schema: "api",
                newName: "ProjectEnvironmentVariables",
                newSchema: "api");

            migrationBuilder.RenameTable(
                name: "ProjectEnvironment",
                schema: "api",
                newName: "ProjectEnvironments",
                newSchema: "api");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectEnvironmentVariable_ProjectEnvironmentId",
                schema: "api",
                table: "ProjectEnvironmentVariables",
                newName: "IX_ProjectEnvironmentVariables_ProjectEnvironmentId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectEnvironment_ProjectId",
                schema: "api",
                table: "ProjectEnvironments",
                newName: "IX_ProjectEnvironments_ProjectId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectEnvironmentId",
                schema: "api",
                table: "ProjectEnvironmentVariables",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectEnvironmentVariables",
                schema: "api",
                table: "ProjectEnvironmentVariables",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectEnvironments",
                schema: "api",
                table: "ProjectEnvironments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEnvironments_Projects_ProjectId",
                schema: "api",
                table: "ProjectEnvironments",
                column: "ProjectId",
                principalSchema: "api",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEnvironmentVariables_ProjectEnvironments_ProjectEnvi~",
                schema: "api",
                table: "ProjectEnvironmentVariables",
                column: "ProjectEnvironmentId",
                principalSchema: "api",
                principalTable: "ProjectEnvironments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEnvironments_Projects_ProjectId",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEnvironmentVariables_ProjectEnvironments_ProjectEnvi~",
                schema: "api",
                table: "ProjectEnvironmentVariables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectEnvironmentVariables",
                schema: "api",
                table: "ProjectEnvironmentVariables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectEnvironments",
                schema: "api",
                table: "ProjectEnvironments");

            migrationBuilder.RenameTable(
                name: "ProjectEnvironmentVariables",
                schema: "api",
                newName: "ProjectEnvironmentVariable",
                newSchema: "api");

            migrationBuilder.RenameTable(
                name: "ProjectEnvironments",
                schema: "api",
                newName: "ProjectEnvironment",
                newSchema: "api");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectEnvironmentVariables_ProjectEnvironmentId",
                schema: "api",
                table: "ProjectEnvironmentVariable",
                newName: "IX_ProjectEnvironmentVariable_ProjectEnvironmentId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectEnvironments_ProjectId",
                schema: "api",
                table: "ProjectEnvironment",
                newName: "IX_ProjectEnvironment_ProjectId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectEnvironmentId",
                schema: "api",
                table: "ProjectEnvironmentVariable",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectEnvironmentVariable",
                schema: "api",
                table: "ProjectEnvironmentVariable",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectEnvironment",
                schema: "api",
                table: "ProjectEnvironment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEnvironment_Projects_ProjectId",
                schema: "api",
                table: "ProjectEnvironment",
                column: "ProjectId",
                principalSchema: "api",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEnvironmentVariable_ProjectEnvironment_ProjectEnviro~",
                schema: "api",
                table: "ProjectEnvironmentVariable",
                column: "ProjectEnvironmentId",
                principalSchema: "api",
                principalTable: "ProjectEnvironment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
