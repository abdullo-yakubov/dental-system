namespace DentalLocal.Shared.Contracts.Warranties;

public sealed record WarrantyDto(
    Guid Id,
    Guid PatientId,
    Guid VisitId,
    Guid ProcedureId,
    string ProcedureName,
    short? ToothNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes);
