using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialsAndInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "materials",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity_in_stock = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    min_stock_threshold = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    price_per_unit = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "material_usages",
                schema: "dental",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_used = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_usages_materials_material_id",
                        column: x => x.material_id,
                        principalSchema: "dental",
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_material_usages_visits_visit_id",
                        column: x => x.visit_id,
                        principalSchema: "dental",
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_material_usages_material_id",
                schema: "dental",
                table: "material_usages",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_usages_visit_id",
                schema: "dental",
                table: "material_usages",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "ix_materials_name",
                schema: "dental",
                table: "materials",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "material_usages",
                schema: "dental");

            migrationBuilder.DropTable(
                name: "materials",
                schema: "dental");
        }
    }
}
