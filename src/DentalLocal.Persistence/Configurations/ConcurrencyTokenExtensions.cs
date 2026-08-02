using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalLocal.Persistence.Configurations;

public static class ConcurrencyTokenExtensions
{
    /// <summary>
    /// Отображает переданное свойство-версию на системную колонку PostgreSQL "xmin" (идентификатор
    /// транзакции, обновляемый движком СУБД при каждом UPDATE строки) и делает её concurrency token —
    /// оптимистичная блокировка "из коробки", без собственных триггеров и ручного инкремента версии.
    ///
    /// ВАЖНО: колонка "xmin" уже физически существует в любой таблице Postgres как системная.
    /// При генерации EF Core миграции для нового применения этого метода нужно вручную убрать
    /// сгенерированные AddColumn/DropColumn для колонки "xmin" — иначе миграция упадёт с ошибкой
    /// "column name "xmin" conflicts with a system column name".
    /// </summary>
    public static void UseXminAsConcurrencyToken<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, uint>> versionProperty)
        where TEntity : class
    {
        builder.Property(versionProperty)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
