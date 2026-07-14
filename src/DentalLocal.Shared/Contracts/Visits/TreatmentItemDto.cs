namespace DentalLocal.Shared.Contracts.Visits;

public sealed record TreatmentItemDto(
    Guid Id,
    Guid ProcedureId,
    string ProcedureName,
    short? ToothNumber,
    decimal Price,
    int Quantity,
    string? Comment);
