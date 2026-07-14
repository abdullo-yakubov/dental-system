namespace DentalLocal.Shared.Contracts.Procedures;

public sealed record ProcedureCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? Color);

public sealed record ProcedureDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int? DurationMinutes,
    string? Color,
    Guid? CategoryId,
    string? CategoryName,
    bool RequiresTooth,
    bool IsActive);
