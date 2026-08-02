using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Persistence;

public static class MigrationExtensions
{
    /// <summary>Advisory-lock PostgreSQL, под которым применяются миграции — исключает гонку при параллельном старте (см. п.10 ТЗ).</summary>
    private const long MigrationAdvisoryLockKey = 851234;

    /// <param name="seedTestPatients">Только для Development: доращивает базу до 100 разнообразных тестовых
    /// пациентов с визитами/оплатами (см. DbSeeder.SeedTestPatientsAsync) — передаётся из Program.cs через
    /// env.IsDevelopment(), чтобы не засорять тестовыми данными базу установленной клиники.</param>
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider, bool seedTestPatients = false, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DentalDbContext>();

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using (var lockCommand = context.Database.GetDbConnection().CreateCommand())
            {
                lockCommand.CommandText = $"SELECT pg_advisory_lock({MigrationAdvisoryLockKey})";
                await lockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await context.Database.MigrateAsync(cancellationToken);

            var seeder = scope.ServiceProvider.GetRequiredService<Seed.DbSeeder>();
            await seeder.SeedAsync(cancellationToken);

            if (seedTestPatients)
                await seeder.SeedTestPatientsAsync(cancellationToken);
        }
        finally
        {
            await using var unlockCommand = context.Database.GetDbConnection().CreateCommand();
            unlockCommand.CommandText = $"SELECT pg_advisory_unlock({MigrationAdvisoryLockKey})";
            await unlockCommand.ExecuteNonQueryAsync(cancellationToken);

            await context.Database.CloseConnectionAsync();
        }
    }
}
