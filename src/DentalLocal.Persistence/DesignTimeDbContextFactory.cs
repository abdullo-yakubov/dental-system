using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalLocal.Persistence;

/// <summary>Используется только инструментами `dotnet ef` для генерации миграций в design-time.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DentalDbContext>
{
    public DentalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DentalDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("DENTALLOCAL_CONNECTION_STRING")
            ?? "Host=127.0.0.1;Port=5544;Database=dental;Username=dental;Password=design_time_only";

        optionsBuilder
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DentalDbContext.Schema))
            .UseSnakeCaseNamingConvention();

        return new DentalDbContext(optionsBuilder.Options);
    }
}
