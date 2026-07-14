namespace DentalLocal.Shared.Contracts.Patients;

public sealed record PatientTimelineEventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset Timestamp,
    string Icon,
    string Color);
