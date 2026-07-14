using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaStage1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patients_phone",
                schema: "dental",
                table: "patients");

            migrationBuilder.AddColumn<decimal>(
                name: "price_max",
                schema: "dental",
                table: "procedures",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "price_min",
                schema: "dental",
                table: "procedures",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "requires_consent",
                schema: "dental",
                table: "procedures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "specialty",
                schema: "dental",
                table: "procedures",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "warranty_months",
                schema: "dental",
                table: "procedures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "dental",
                table: "patients",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "before_after_type",
                schema: "dental",
                table: "patient_files",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sha256hash",
                schema: "dental",
                table: "patient_files",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "currencies",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    exchange_rate_to_base = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    is_base = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_history",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedules",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doctor_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prescriptions",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    medication_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dosage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prescriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_prescriptions_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "waiting_list",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desired_date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    desired_date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Waiting"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waiting_list", x => x.id);
                    table.ForeignKey(
                        name: "fk_waiting_list_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waiting_list_procedures_procedure_id",
                        column: x => x.procedure_id,
                        principalSchema: "dental",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "warranties",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tooth_number = table.Column<short>(type: "smallint", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warranties", x => x.id);
                    table.ForeignKey(
                        name: "fk_warranties_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warranties_procedures_procedure_id",
                        column: x => x.procedure_id,
                        principalSchema: "dental",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warranties_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patients_first_name_trgm",
                schema: "dental",
                table: "patients",
                column: "first_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_patients_phone_trgm",
                schema: "dental",
                table: "patients",
                column: "phone")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_currencies_code",
                schema: "dental",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_history_patient_id",
                schema: "dental",
                table: "discount_history",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_history_visit_id",
                schema: "dental",
                table: "discount_history",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedules_doctor_name_day_of_week",
                schema: "dental",
                table: "doctor_schedules",
                columns: new[] { "doctor_name", "day_of_week" });

            migrationBuilder.CreateIndex(
                name: "ix_prescriptions_visit_id",
                schema: "dental",
                table: "prescriptions",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_waiting_list_patient_id",
                schema: "dental",
                table: "waiting_list",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_waiting_list_procedure_id",
                schema: "dental",
                table: "waiting_list",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_waiting_list_status",
                schema: "dental",
                table: "waiting_list",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_warranties_patient_id",
                schema: "dental",
                table: "warranties",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_warranties_procedure_id",
                schema: "dental",
                table: "warranties",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_warranties_visit_id",
                schema: "dental",
                table: "warranties",
                column: "visit_id");

            // Custom SQL to create patient_debts view
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW dental.patient_debts AS
                SELECT 
                    p.id AS patient_id,
                    (p.last_name || ' ' || p.first_name || COALESCE(' ' || p.middle_name, '')) AS full_name,
                    p.phone,
                    COALESCE(SUM(v.total_cost - v.total_paid), 0) AS total_debt,
                    MAX(d.arose_at) AS last_debt_date,
                    (SELECT id FROM dental.visits WHERE patient_id = p.id ORDER BY visit_date DESC, visit_time DESC LIMIT 1) AS last_visit_id
                FROM dental.patients p
                LEFT JOIN dental.visits v ON p.id = v.patient_id
                LEFT JOIN dental.debts d ON v.id = d.visit_id
                GROUP BY p.id, p.last_name, p.first_name, p.middle_name, p.phone;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dental.patient_debts;");

            migrationBuilder.DropTable(
                name: "currencies",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "discount_history",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "doctor_schedules",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "prescriptions",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "waiting_list",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "warranties",
                schema: "dental");

            migrationBuilder.DropIndex(
                name: "ix_patients_first_name_trgm",
                schema: "dental",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "ix_patients_phone_trgm",
                schema: "dental",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "price_max",
                schema: "dental",
                table: "procedures");

            migrationBuilder.DropColumn(
                name: "price_min",
                schema: "dental",
                table: "procedures");

            migrationBuilder.DropColumn(
                name: "requires_consent",
                schema: "dental",
                table: "procedures");

            migrationBuilder.DropColumn(
                name: "specialty",
                schema: "dental",
                table: "procedures");

            migrationBuilder.DropColumn(
                name: "warranty_months",
                schema: "dental",
                table: "procedures");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "dental",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "before_after_type",
                schema: "dental",
                table: "patient_files");

            migrationBuilder.DropColumn(
                name: "sha256hash",
                schema: "dental",
                table: "patient_files");

            migrationBuilder.CreateIndex(
                name: "ix_patients_phone",
                schema: "dental",
                table: "patients",
                column: "phone");
        }
    }
}
