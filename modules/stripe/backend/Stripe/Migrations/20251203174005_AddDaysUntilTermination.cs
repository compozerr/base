using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stripe.Migrations
{
    /// <inheritdoc />
    public partial class AddDaysUntilTermination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DaysUntilTermination",
                schema: "stripe",
                table: "PaymentMethodMissingSagas",
                type: "integer",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaysUntilTermination",
                schema: "stripe",
                table: "PaymentMethodMissingSagas");
        }
    }
}
