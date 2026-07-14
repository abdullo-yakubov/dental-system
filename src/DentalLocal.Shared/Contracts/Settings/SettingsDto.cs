namespace DentalLocal.Shared.Contracts.Settings;

public sealed record SettingsDto(
    string DoctorName,
    string ClinicName,
    string Currency,
    string Theme,
    string Language,
    string BackupTime,
    int BackupRetentionDays,
    int AutoLockMinutes);

public sealed record UpdateSettingsRequest(
    string DoctorName,
    string ClinicName,
    string Currency,
    string Theme,
    string Language,
    string BackupTime,
    int BackupRetentionDays,
    int AutoLockMinutes);
