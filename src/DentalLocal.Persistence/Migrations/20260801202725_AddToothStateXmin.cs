using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToothStateXmin : Migration
    {
        // Эта миграция намеренно НЕ содержит операций над схемой — см. AddXminConcurrencyTokens
        // и ConcurrencyTokenExtensions.UseXminAsConcurrencyToken: колонка "xmin" уже физически
        // существует в таблице "tooth_states" как системная, AddColumn/DropColumn для неё
        // сгенерированы EF Core автоматически и удалены вручную (иначе падают с ошибкой
        // "column name \"xmin\" conflicts with a system column name"). Изменение фиксируется
        // только в снапшоте модели (ToothState.Version -> concurrency token).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
