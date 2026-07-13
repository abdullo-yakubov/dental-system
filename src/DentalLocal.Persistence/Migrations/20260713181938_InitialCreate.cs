using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dental");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "app_settings",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "backup_records",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    trigger = table.Column<short>(type: "smallint", nullable: false),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_backup_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "procedure_categories",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "procedures",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    requires_tooth = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedures", x => x.id);
                    table.CheckConstraint("ck_procedures_price_non_negative", "price >= 0");
                    table.ForeignKey(
                        name: "fk_procedures_procedure_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "dental",
                        principalTable: "procedure_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_appointments", x => x.id);
                    table.ForeignKey(
                        name: "fk_appointments_procedures_procedure_id",
                        column: x => x.procedure_id,
                        principalSchema: "dental",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "debts",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    arose_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_debts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patient_files",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_type = table.Column<short>(type: "smallint", nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    stored_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patient_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gender = table.Column<short>(type: "smallint", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    phone_extra = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    allergies = table.Column<string>(type: "text", nullable: true),
                    chronic_diseases = table.Column<string>(type: "text", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true),
                    doctor_notes = table.Column<string>(type: "text", nullable: true),
                    photo_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_visit_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('russian', coalesce(last_name, '') || ' ' || coalesce(first_name, '') || ' ' || coalesce(middle_name, '') || ' ' || coalesce(phone, '') || ' ' || coalesce(address, ''))", stored: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patients", x => x.id);
                    table.ForeignKey(
                        name: "fk_patients_patient_files_photo_file_id",
                        column: x => x.photo_file_id,
                        principalSchema: "dental",
                        principalTable: "patient_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tooth_states",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tooth_number = table.Column<short>(type: "smallint", nullable: false),
                    state = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tooth_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_tooth_states_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visits",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_date = table.Column<DateOnly>(type: "date", nullable: false),
                    visit_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    doctor_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    prescriptions = table.Column<string>(type: "text", nullable: true),
                    materials_used = table.Column<string>(type: "text", nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_paid = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visits", x => x.id);
                    table.ForeignKey(
                        name: "fk_visits_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    method = table.Column<short>(type: "smallint", nullable: false),
                    kind = table.Column<short>(type: "smallint", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    reversal_of_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payments_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_payments_reversal_of_payment_id",
                        column: x => x.reversal_of_payment_id,
                        principalSchema: "dental",
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tooth_state_history",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tooth_number = table.Column<short>(type: "smallint", nullable: false),
                    old_state = table.Column<short>(type: "smallint", nullable: false),
                    new_state = table.Column<short>(type: "smallint", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tooth_state_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_tooth_state_history_patients_patient_id",
                        column: x => x.patient_id,
                        principalSchema: "dental",
                        principalTable: "patients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tooth_state_history_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "treatment_items",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tooth_number = table.Column<short>(type: "smallint", nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_treatment_items", x => x.id);
                    table.CheckConstraint("ck_treatment_items_tooth_number", "tooth_number IS NULL OR tooth_number IN (11,12,13,14,15,16,17,18,21,22,23,24,25,26,27,28,31,32,33,34,35,36,37,38,41,42,43,44,45,46,47,48)");
                    table.ForeignKey(
                        name: "fk_treatment_items_procedures_procedure_id",
                        column: x => x.procedure_id,
                        principalSchema: "dental",
                        principalTable: "procedures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_treatment_items_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_settings_key",
                schema: "dental",
                table: "app_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointments_patient_id",
                schema: "dental",
                table: "appointments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_procedure_id",
                schema: "dental",
                table: "appointments",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_starts_at",
                schema: "dental",
                table: "appointments",
                column: "starts_at");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_visit_id",
                schema: "dental",
                table: "appointments",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_action",
                schema: "dental",
                table: "audit_log",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_occurred_at",
                schema: "dental",
                table: "audit_log",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_backup_records_created_at",
                schema: "dental",
                table: "backup_records",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_debts_patient_id",
                schema: "dental",
                table: "debts",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_debts_status",
                schema: "dental",
                table: "debts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_debts_visit_id",
                schema: "dental",
                table: "debts",
                column: "visit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_files_patient_id",
                schema: "dental",
                table: "patient_files",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_card_number",
                schema: "dental",
                table: "patients",
                column: "card_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patients_last_name_first_name",
                schema: "dental",
                table: "patients",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_patients_last_name_trgm",
                schema: "dental",
                table: "patients",
                column: "last_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_patients_not_archived",
                schema: "dental",
                table: "patients",
                column: "is_archived",
                filter: "NOT is_archived");

            migrationBuilder.CreateIndex(
                name: "ix_patients_phone",
                schema: "dental",
                table: "patients",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_patients_photo_file_id",
                schema: "dental",
                table: "patients",
                column: "photo_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_search_vector",
                schema: "dental",
                table: "patients",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_payments_patient_id",
                schema: "dental",
                table: "payments",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_reversal_of_payment_id",
                schema: "dental",
                table: "payments",
                column: "reversal_of_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_visit_id",
                schema: "dental",
                table: "payments",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedure_categories_name",
                schema: "dental",
                table: "procedure_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_procedures_category_id",
                schema: "dental",
                table: "procedures",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_procedures_is_active",
                schema: "dental",
                table: "procedures",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_tooth_state_history_patient_id_tooth_number",
                schema: "dental",
                table: "tooth_state_history",
                columns: new[] { "patient_id", "tooth_number" });

            migrationBuilder.CreateIndex(
                name: "ix_tooth_state_history_visit_id",
                schema: "dental",
                table: "tooth_state_history",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_tooth_states_patient_id_tooth_number",
                schema: "dental",
                table: "tooth_states",
                columns: new[] { "patient_id", "tooth_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_treatment_items_procedure_id",
                schema: "dental",
                table: "treatment_items",
                column: "procedure_id");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_items_visit_id",
                schema: "dental",
                table: "treatment_items",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_visits_patient_id",
                schema: "dental",
                table: "visits",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_visits_visit_date",
                schema: "dental",
                table: "visits",
                column: "visit_date");

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_patients_patient_id",
                schema: "dental",
                table: "appointments",
                column: "patient_id",
                principalSchema: "dental",
                principalTable: "patients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_appointments_visits_visit_id",
                schema: "dental",
                table: "appointments",
                column: "visit_id",
                principalSchema: "dental",
                principalTable: "visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_debts_patients_patient_id",
                schema: "dental",
                table: "debts",
                column: "patient_id",
                principalSchema: "dental",
                principalTable: "patients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_debts_visits_visit_id",
                schema: "dental",
                table: "debts",
                column: "visit_id",
                principalSchema: "dental",
                principalTable: "visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_patient_files_patients_patient_id",
                schema: "dental",
                table: "patient_files",
                column: "patient_id",
                principalSchema: "dental",
                principalTable: "patients",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_patient_files_patients_patient_id",
                schema: "dental",
                table: "patient_files");

            migrationBuilder.DropTable(
                name: "app_settings",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "appointments",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "backup_records",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "debts",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "tooth_state_history",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "tooth_states",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "treatment_items",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "procedures",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "visits",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "procedure_categories",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "patients",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "patient_files",
                schema: "dental");
        }
    }
}
