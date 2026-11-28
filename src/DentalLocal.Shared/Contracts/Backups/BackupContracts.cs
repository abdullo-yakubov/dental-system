namespace DentalLocal.Shared.Contracts.Backups;

public sealed record BackupDto(
    string FileName,
    DateTimeOffset CreatedAt,
    long SizeBytes,
    short Trigger,
    bool IsValid,
    string? Note);

public sealed record CreateBackupRequest(string Pin, string? Note);

public sealed record RestoreBackupRequest(string Pin, string BackupFileName);
