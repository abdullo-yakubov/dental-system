using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitNotesSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "notes_search_vector",
                schema: "dental",
                table: "visits",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('russian', coalesce(notes, '') || ' ' || coalesce(diagnosis, ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_visits_notes_search_vector",
                schema: "dental",
                table: "visits",
                column: "notes_search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_visits_notes_search_vector",
                schema: "dental",
                table: "visits");

            migrationBuilder.DropColumn(
                name: "notes_search_vector",
                schema: "dental",
                table: "visits");
        }
    }
}
