using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.Visits;

public sealed record VisitDto(
    Guid Id,
    Guid PatientId,
    string PatientFullName,
    DateOnly VisitDate,
    TimeOnly VisitTime,
    string DoctorName,
    string? Notes,
    string? Prescriptions,
    string? MaterialsUsed,
    decimal TotalCost,
    decimal TotalPaid,
    decimal RemainingAmount,
    VisitStatus Status,
    IReadOnlyList<TreatmentItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
