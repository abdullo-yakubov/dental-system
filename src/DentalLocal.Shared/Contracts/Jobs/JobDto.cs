namespace DentalLocal.Shared.Contracts.Jobs;

public record JobDto(
    Guid Id,
    string Type,
    string Status,
    int Progress,
    string? Error,
    DateTime CreatedAt,
    DateTime? FinishedAt);
