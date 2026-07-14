using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.TreatmentPlans;

public sealed record TreatmentPlanItemDto(
    Guid Id,
    Guid ProcedureId,
    string ProcedureName,
    short? ToothNumber,
    decimal EstimatedPrice,
    int OrderIndex,
    bool IsCompleted,
    Guid? CompletedVisitId);

public sealed record TreatmentPlanDto(
    Guid Id,
    Guid PatientId,
    string Title,
    string? Notes,
    TreatmentPlanStatus Status,
    decimal TotalEstimatedPrice,
    int CompletedCount,
    IReadOnlyList<TreatmentPlanItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateTreatmentPlanRequest(
    Guid PatientId,
    string Title,
    string? Notes);

public sealed record UpdateTreatmentPlanRequest(
    string Title,
    string? Notes);

public sealed record AddTreatmentPlanItemRequest(
    Guid ProcedureId,
    short? ToothNumber,
    decimal EstimatedPrice,
    int OrderIndex);

public sealed record CompleteTreatmentPlanItemRequest(
    Guid VisitId);
