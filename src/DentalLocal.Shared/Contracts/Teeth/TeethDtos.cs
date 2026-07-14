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

public sealed record ToothSurfaceStateDto(
    short ToothNumber,
    string SurfaceCode,
    ToothStateType Status);

public sealed record OdontogramDto(
    Guid PatientId,
    IReadOnlyList<ToothStateDto> Teeth,
    IReadOnlyList<ToothStateHistoryDto> RecentHistory,
    IReadOnlyList<ToothSurfaceStateDto> SurfaceStates);

public sealed record UpdateToothRequest(
    short ToothNumber,
    ToothStateType NewState,
    Guid? VisitId,
    string? Comment);

public sealed record ToothSurfaceHistoryDto(
    Guid Id,
    short ToothNumber,
    string SurfaceCode,
    ToothStateType Status,
    string? Diagnosis,
    string? Icd10Code,
    string? Icd11Code,
    Guid? VisitId,
    DateTimeOffset ChangedAt,
    string? DoctorComment);

public sealed record AddToothSurfaceRecordRequest(
    string SurfaceCode,
    ToothStateType Status,
    string? Diagnosis,
    string? Icd10Code,
    string? Icd11Code,
    Guid? VisitId,
    string? DoctorComment);

/// <summary>Единая хронологическая запись для ленты истории зуба (объединяет ToothStateHistory и ToothSurfaceHistory).</summary>
public sealed record ToothTimelineEntryDto(
    short ToothNumber,
    string? SurfaceCode,
    ToothStateType Status,
    string? Diagnosis,
    string? Icd10Code,
    string? Icd11Code,
    Guid? VisitId,
    DateTimeOffset ChangedAt,
    string? Comment);
