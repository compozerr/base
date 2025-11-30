using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stripe.Migrations
{
    /// <inheritdoc />
    public partial class Updatedsagadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentMethodMissingSagaAuditLogs",
                schema: "stripe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SagaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Event = table.Column<int>(type: "integer", nullable: false),
                    EventTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdditionalData = table.Column<string>(type: "text", nullable: true),
                    JobId = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodMissingSagaAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethodMissingSagas",
                schema: "stripe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<int>(type: "integer", nullable: true),
                    FirstWarningJobId = table.Column<string>(type: "text", nullable: true),
                    SecondWarningJobId = table.Column<string>(type: "text", nullable: true),
                    TerminationJobId = table.Column<string>(type: "text", nullable: true),
                    FirstWarningSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecondWarningSentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodMissingSagas", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethodMissingSagaAuditLogs",
                schema: "stripe");

            migrationBuilder.DropTable(
                name: "PaymentMethodMissingSagas",
                schema: "stripe");
        }
    }
}
