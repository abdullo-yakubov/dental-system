using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDebtRemainingAmountCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_debts_remaining_amount_bounds",
                schema: "dental",
                table: "debts",
                sql: "remaining_amount >= 0 AND remaining_amount <= original_amount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_debts_remaining_amount_bounds",
                schema: "dental",
                table: "debts");
        }
    }
}
