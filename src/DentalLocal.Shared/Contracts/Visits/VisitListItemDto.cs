using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.Visits;

public sealed record VisitListItemDto(
    Guid Id,
    Guid PatientId,
    string PatientFullName,
    DateOnly VisitDate,
    TimeOnly VisitTime,
    string DoctorName,
    decimal TotalCost,
    decimal TotalPaid,
    decimal RemainingAmount,
    VisitStatus Status,
    int ProcedureCount,
    int DurationMinutes);
