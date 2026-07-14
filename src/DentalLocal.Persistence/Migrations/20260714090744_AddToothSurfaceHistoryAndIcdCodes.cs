using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToothSurfaceHistoryAndIcdCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "icd10_codes",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_icd10_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "icd11_codes",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_icd11_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tooth_surface_history",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tooth_id = table.Column<Guid>(type: "uuid", nullable: false),
                    surface_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    diagnosis = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    icd10code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    icd11code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    doctor_comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tooth_surface_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_tooth_surface_history_tooth_states_tooth_id",
                        column: x => x.tooth_id,
                        principalSchema: "dental",
                        principalTable: "tooth_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tooth_surface_history_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_icd10_codes_code",
                schema: "dental",
                table: "icd10_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_icd11_codes_code",
                schema: "dental",
                table: "icd11_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tooth_surface_history_changed_at",
                schema: "dental",
                table: "tooth_surface_history",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_tooth_surface_history_tooth_id_surface_code",
                schema: "dental",
                table: "tooth_surface_history",
                columns: new[] { "tooth_id", "surface_code" });

            migrationBuilder.CreateIndex(
                name: "ix_tooth_surface_history_visit_id",
                schema: "dental",
                table: "tooth_surface_history",
                column: "visit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "icd10_codes",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "icd11_codes",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "tooth_surface_history",
                schema: "dental");
        }
    }
}
