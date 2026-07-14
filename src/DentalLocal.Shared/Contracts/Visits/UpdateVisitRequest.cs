namespace DentalLocal.Shared.Contracts.Visits;

public sealed record UpdateVisitRequest(
    DateOnly VisitDate,
    TimeOnly VisitTime,
    string DoctorName,
    string? Notes,
    string? Prescriptions,
    string? MaterialsUsed);
