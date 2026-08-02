using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

/// <summary>
/// Факт оплаты. Физическое удаление и изменение запрещены — сторнирование
/// выполняется отдельной корректирующей записью (<see cref="ReversalOfPaymentId"/>).
/// </summary>
public class Payment : Entity, IImmutableRecord
{
    public Guid VisitId { get; private set; }
    public Visit Visit { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentKind Kind { get; private set; }
    public string? Note { get; private set; }
    public Guid? ReversalOfPaymentId { get; private set; }

    /// <summary>Concurrency token, отображается на системную колонку Postgres "xmin" (см. PaymentConfiguration).</summary>
    public uint Version { get; private set; }

    private Payment()
    {
    }

    public static Payment Create(
        Guid visitId,
        Guid patientId,
        decimal amount,
        PaymentMethod method,
        PaymentKind kind,
        string? note)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма оплаты должна быть положительной.");

        return new Payment
        {
            VisitId = visitId,
            PatientId = patientId,
            Amount = amount,
            Method = method,
            Kind = kind,
            Note = note,
            PaidAt = DateTimeOffset.UtcNow,
        };
    }

    public static Payment CreateReversal(Payment original, string? note)
    {
        return new Payment
        {
            VisitId = original.VisitId,
            PatientId = original.PatientId,
            Amount = original.Amount,
            Method = original.Method,
            Kind = original.Kind,
            Note = note,
            PaidAt = DateTimeOffset.UtcNow,
            ReversalOfPaymentId = original.Id,
        };
    }
}
