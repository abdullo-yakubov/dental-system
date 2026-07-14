namespace DentalLocal.Shared.Contracts.Patients;

public sealed record PatientFileDto(
    Guid Id,
    Guid PatientId,
    short FileType,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StoredPath,
    DateTimeOffset UploadedAt,
    string? Description);
