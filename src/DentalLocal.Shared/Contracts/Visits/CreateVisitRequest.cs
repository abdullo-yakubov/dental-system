namespace DentalLocal.Shared.Contracts.Visits;

public sealed record CreateTreatmentItemRequest(
    Guid ProcedureId,
    short? ToothNumber,
    decimal Price,
    int Quantity,
    string? Comment);

public sealed record CreateVisitRequest(
    Guid PatientId,
    DateOnly VisitDate,
    TimeOnly VisitTime,
    string DoctorName,
    string? Notes,
    string? Prescriptions,
    string? MaterialsUsed,
    IReadOnlyList<CreateTreatmentItemRequest> Items);
