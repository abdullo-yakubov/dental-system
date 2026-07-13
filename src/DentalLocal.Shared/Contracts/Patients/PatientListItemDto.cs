namespace DentalLocal.Shared.Contracts.Patients;

public sealed record PatientListItemDto(
    Guid Id,
    string CardNumberDisplay,
    string FullName,
    int Age,
    string Phone,
    Guid? PhotoFileId,
    DateTimeOffset? LastVisitAt,
    bool IsArchived);
