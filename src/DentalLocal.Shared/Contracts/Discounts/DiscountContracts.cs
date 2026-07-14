namespace DentalLocal.Shared.Contracts.Discounts;

public sealed record DiscountHistoryDto(
    Guid Id,
    Guid PatientId,
    Guid? VisitId,
    string DiscountType,
    decimal Amount,
    decimal Percent,
    string? Reason,
    DateTimeOffset AppliedAt);

public sealed record ApplyDiscountRequest(
    Guid? VisitId,
    string DiscountType,
    decimal Amount,
    decimal Percent,
    string? Reason);
