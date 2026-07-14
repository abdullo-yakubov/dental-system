using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.Payments;

public sealed record PaymentDto(
    Guid Id,
    Guid VisitId,
    Guid PatientId,
    string PatientFullName,
    decimal Amount,
    DateTimeOffset PaidAt,
    PaymentMethod Method,
    PaymentKind Kind,
    string? Note,
    Guid? ReversalOfPaymentId);

public sealed record DebtDto(
    Guid Id,
    Guid VisitId,
    Guid PatientId,
    string PatientFullName,
    decimal OriginalAmount,
    decimal RemainingAmount,
    DateTimeOffset AroseAt,
    DebtStatus Status,
    DateTimeOffset? ClosedAt);

public sealed record DebtListItemDto(
    Guid Id,
    Guid VisitId,
    Guid PatientId,
    string PatientFullName,
    string PatientPhone,
    DateOnly PatientBirthDate,
    decimal OriginalAmount,
    decimal RemainingAmount,
    DateTimeOffset AroseAt,
    DebtStatus Status,
    DateOnly VisitDate);

public sealed record RegisterPaymentRequest(
    Guid VisitId,
    Guid PatientId,
    decimal Amount,
    PaymentMethod Method,
    PaymentKind Kind,
    string? Note);
