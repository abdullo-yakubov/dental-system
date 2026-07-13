using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.Patients;

public sealed record PatientDto(
    Guid Id,
    int CardNumber,
    string CardNumberDisplay,
    string LastName,
    string FirstName,
    string? MiddleName,
    string FullName,
    Gender Gender,
    DateOnly BirthDate,
    int Age,
    string Phone,
    string? PhoneExtra,
    string? Address,
    string? Allergies,
    string? ChronicDiseases,
    string? Comments,
    string? DoctorNotes,
    Guid? PhotoFileId,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastVisitAt,
    bool IsArchived);
