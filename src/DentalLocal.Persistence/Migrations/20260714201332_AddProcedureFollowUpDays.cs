using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcedureFollowUpDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "follow_up_days",
                schema: "dental",
                table: "procedures",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "follow_up_days",
                schema: "dental",
                table: "procedures");
        }
    }
}
