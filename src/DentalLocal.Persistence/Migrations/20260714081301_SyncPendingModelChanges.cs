using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treatment_plans",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_treatment_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_treatment_plans_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "treatment_plan_items",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    treatment_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tooth_number = table.Column<short>(type: "smallint", nullable: true),
                    estimated_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_treatment_plan_items", x => x.id);
                    table.CheckConstraint("ck_treatment_plan_items_tooth_number", "tooth_number IS NULL OR tooth_number IN (11,12,13,14,15,16,17,18,21,22,23,24,25,26,27,28,31,32,33,34,35,36,37,38,41,42,43,44,45,46,47,48)");
                    table.ForeignKey(
                        name: "fk_treatment_plan_items_procedures_procedure_id",
                        column: x => x.procedure_id,
                        principalSchema: "dental",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_treatment_plan_items_treatment_plans_treatment_plan_id",
                        column: x => x.treatment_plan_id,
                        principalSchema: "dental",
                        principalTable: "treatment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plan_items_procedure_id",
                schema: "dental",
                table: "treatment_plan_items",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plan_items_treatment_plan_id",
                schema: "dental",
                table: "treatment_plan_items",
                column: "treatment_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plans_patient_id",
                schema: "dental",
                table: "treatment_plans",
                column: "patient_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treatment_plan_items",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "treatment_plans",
                schema: "dental");
        }
    }
}
