namespace DentalLocal.Shared.Contracts.Patients;

/// <summary>Короткоживущая подписанная ссылка на файл пациента — см. GetPatientFileByIdQuery / FilesEndpoints.</summary>
public sealed record FileLinkDto(string Url, DateTimeOffset ExpiresAt);
