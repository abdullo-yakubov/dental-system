namespace DentalLocal.Shared.Contracts.Backups;

public sealed record BackupDto(
    Guid Id, string FileName, long SizeBytes, short Trigger,
    bool IsValid, string? Note, DateTimeOffset CreatedAt);

public sealed record CreateBackupRequest(string Pin, string? Note);

public sealed record RestoreBackupRequest(string Pin, string BackupFileName);
