using DentalLocal.Application.Common;

namespace DentalLocal.Persistence.Services;

internal sealed class DatabaseMigrator(IServiceProvider serviceProvider) : IDatabaseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        serviceProvider.MigrateDatabaseAsync(cancellationToken: cancellationToken);
}
