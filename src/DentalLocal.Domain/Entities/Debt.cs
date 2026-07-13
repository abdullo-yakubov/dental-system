using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Exceptions;

namespace DentalLocal.Domain.Entities;

public class Debt : Entity, IImmutableRecord
{
    public Guid VisitId { get; private set; }
    public Visit Visit { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public decimal OriginalAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public DateTimeOffset AroseAt { get; private set; }
    public DebtStatus Status { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private Debt()
    {
    }

    public static Debt Create(Guid visitId, Guid patientId, decimal originalAmount)
    {
        if (originalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(originalAmount), "Сумма долга должна быть положительной.");

        return new Debt
        {
            VisitId = visitId,
            PatientId = patientId,
            OriginalAmount = originalAmount,
            RemainingAmount = originalAmount,
            AroseAt = DateTimeOffset.UtcNow,
            Status = DebtStatus.Open,
        };
    }

    public void ApplyPayment(decimal amount)
    {
        if (Status == DebtStatus.Closed)
            throw new DomainException("Долг уже погашен.");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма оплаты должна быть положительной.");

        if (amount > RemainingAmount)
            throw new DomainException($"Сумма оплаты ({amount}) превышает остаток долга ({RemainingAmount}).");

        RemainingAmount -= amount;
        Touch();

        if (RemainingAmount == 0)
            Close();
    }

    private void Close()
    {
        Status = DebtStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
