namespace DentalLocal.Application.Common;

/// <summary>
/// Service responsible for managing PostgreSQL database backup and restore operations with AES-256 encryption.
/// </summary>
public interface IBackupService
{
    Task<string> CreateBackupAsync(string pin, string? note = null, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string pin, string backupFileName, CancellationToken cancellationToken = default);
    Task<bool> ValidateBackupIntegrityAsync(string backupFileName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Абстракция над применением EF Core миграций — нужна Infrastructure (BackupService.RestoreBackupAsync,
/// который обязан прогнать миграции после pg_restore), но Infrastructure не может напрямую ссылаться
/// на Persistence по правилам слоёв (см. CLAUDE.md), поэтому реализация внедряется из Persistence.
/// </summary>
public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
