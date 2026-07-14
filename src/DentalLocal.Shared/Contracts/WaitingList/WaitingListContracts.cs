namespace DentalLocal.Shared.Contracts.WaitingList;

public sealed record WaitingListEntryDto(
    Guid Id,
    Guid PatientId,
    string PatientFullName,
    DateOnly DesiredDateFrom,
    DateOnly DesiredDateTo,
    Guid? ProcedureId,
    string? ProcedureName,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record CreateWaitingListEntryRequest(
    Guid PatientId,
    DateOnly DesiredDateFrom,
    DateOnly DesiredDateTo,
    Guid? ProcedureId,
    string? Notes);

public sealed record UpdateWaitingListStatusRequest(string Status);
