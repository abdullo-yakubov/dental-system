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
