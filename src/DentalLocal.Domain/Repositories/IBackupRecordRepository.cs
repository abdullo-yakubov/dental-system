using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IBackupRecordRepository : IRepository<BackupRecord>
{
    Task<IReadOnlyList<BackupRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BackupRecord?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupRecord>> GetOlderThanAsync(DateTimeOffset threshold, CancellationToken cancellationToken = default);
}
