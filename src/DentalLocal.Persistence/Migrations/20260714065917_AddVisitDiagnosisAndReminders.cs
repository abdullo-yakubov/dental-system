using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitDiagnosisAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "diagnosis",
                schema: "dental",
                table: "visits",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reminders",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reminder_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_reminders_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reminders_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reminders_patient_id",
                schema: "dental",
                table: "reminders",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_reminder_date",
                schema: "dental",
                table: "reminders",
                column: "reminder_date");

            migrationBuilder.CreateIndex(
                name: "ix_reminders_visit_id",
                schema: "dental",
                table: "reminders",
                column: "visit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reminders",
                schema: "dental");

            migrationBuilder.DropColumn(
                name: "diagnosis",
                schema: "dental",
                table: "visits");
        }
    }
}
