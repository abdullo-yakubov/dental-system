namespace DentalLocal.Shared.Contracts.Prescriptions;

public sealed record PrescriptionDto(
    Guid Id,
    Guid VisitId,
    string MedicationName,
    string Dosage,
    string? Instructions,
    DateTimeOffset CreatedAt);

public sealed record CreatePrescriptionRequest(
    string MedicationName,
    string Dosage,
    string? Instructions);
