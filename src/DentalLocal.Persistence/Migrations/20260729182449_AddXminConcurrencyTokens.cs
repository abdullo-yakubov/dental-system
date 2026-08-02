using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLocal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddXminConcurrencyTokens : Migration
    {
        // Эта миграция намеренно НЕ содержит операций над схемой.
        //
        // Свойства-версии (Material/Payment/ToothSurfaceHistory/Visit.Version) отображены на системную
        // колонку PostgreSQL "xmin" через ConcurrencyTokenExtensions.UseXminAsConcurrencyToken(...).
        // Колонка "xmin" физически существует в КАЖДОЙ таблице Postgres как системная — попытка её
        // ADD/DROP падает с ошибкой "column name \"xmin\" conflicts with a system column name".
        //
        // Поэтому сгенерированные EF Core AddColumn/DropColumn для "xmin" удалены вручную (см. комментарий
        // в ConcurrencyTokenExtensions). Изменение существует только в снапшоте модели — оно фиксирует
        // concurrency-token, но не требует правки БД.

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
