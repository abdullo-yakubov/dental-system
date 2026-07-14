using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.Teeth;

public sealed record ToothStateDto(
    Guid Id,
    Guid PatientId,
    short ToothNumber,
    ToothStateType State,
    DateTimeOffset UpdatedAt);

public sealed record ToothStateHistoryDto(
    Guid Id,
    short ToothNumber,
    ToothStateType OldState,
    ToothStateType NewState,
    DateTimeOffset ChangedAt,
    Guid? VisitId,
    string? Comment);

public sealed record OdontogramDto(
    Guid PatientId,
    IReadOnlyList<ToothStateDto> Teeth,
    IReadOnlyList<ToothStateHistoryDto> RecentHistory);

public sealed record UpdateToothRequest(
    short ToothNumber,
    ToothStateType NewState,
    Guid? VisitId,
    string? Comment);
