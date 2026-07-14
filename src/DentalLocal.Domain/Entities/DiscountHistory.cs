using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class DiscountHistory : Entity, IImmutableRecord
{
    public Guid PatientId { get; private set; }
    public Guid? VisitId { get; private set; }
    public string DiscountType { get; private set; } = null!; // e.g. "Procedure", "Visit", "Patient"
    public decimal Amount { get; private set; }
    public decimal Percent { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }

    private DiscountHistory()
    {
    }

    public static DiscountHistory Create(Guid patientId, Guid? visitId, string discountType, decimal amount, decimal percent, string? reason)
    {
        if (string.IsNullOrWhiteSpace(discountType))
            throw new ArgumentException("Тип скидки обязателен.", nameof(discountType));

        return new DiscountHistory
        {
            PatientId = patientId,
            VisitId = visitId,
            DiscountType = discountType.Trim(),
            Amount = amount,
            Percent = percent,
            Reason = reason,
            AppliedAt = DateTimeOffset.UtcNow
        };
    }
}
