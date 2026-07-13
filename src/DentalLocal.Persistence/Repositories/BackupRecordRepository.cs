using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class BackupRecordRepository(DentalDbContext context) : RepositoryBase<BackupRecord>(context), IBackupRecordRepository
{
    public async Task<IReadOnlyList<BackupRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().OrderByDescending(b => b.CreatedAt).ToListAsync(cancellationToken);

    public Task<BackupRecord?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().OrderByDescending(b => b.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<BackupRecord>> GetOlderThanAsync(DateTimeOffset threshold, CancellationToken cancellationToken = default) =>
        await Set.Where(b => b.CreatedAt < threshold).ToListAsync(cancellationToken);
}
