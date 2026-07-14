namespace DentalLocal.Shared.Contracts.Procedures;

public sealed record CreateProcedureRequest(
    string Name,
    string? Description,
    decimal Price,
    int? DurationMinutes,
    Guid? CategoryId,
    bool RequiresTooth,
    bool IsActive = true);

public sealed record UpdateProcedureRequest(
    string Name,
    string? Description,
    decimal Price,
    int? DurationMinutes,
    Guid? CategoryId,
    bool RequiresTooth,
    bool IsActive);

public sealed record CreateProcedureCategoryRequest(
    string Name,
    string? Description,
    string? Color);
