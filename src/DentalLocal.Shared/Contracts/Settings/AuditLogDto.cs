namespace DentalLocal.Shared.Contracts.Settings;

public sealed record AuditLogDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string Action,
    string? Entity,
    Guid? EntityId,
    string? DetailsJson);
