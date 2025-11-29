using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stripe.Migrations
{
    /// <inheritdoc />
    public partial class Addedsaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentFailureSagaAuditLogs",
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
                    table.PrimaryKey("PK_PaymentFailureSagaAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentFailureSagas",
                schema: "stripe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<string>(type: "text", nullable: false),
                    InvoiceId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<int>(type: "integer", nullable: true),
                    AmountDue = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    PaymentLink = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_PaymentFailureSagas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailureSagaAuditLogs_SagaId",
                schema: "stripe",
                table: "PaymentFailureSagaAuditLogs",
                column: "SagaId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailureSagas_SubscriptionId",
                schema: "stripe",
                table: "PaymentFailureSagas",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailureSagas_SubscriptionId_Status",
                schema: "stripe",
                table: "PaymentFailureSagas",
                columns: new[] { "SubscriptionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentFailureSagaAuditLogs",
                schema: "stripe");

            migrationBuilder.DropTable(
                name: "PaymentFailureSagas",
                schema: "stripe");
        }
    }
}
