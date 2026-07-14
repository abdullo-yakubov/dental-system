using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class MaterialUsage : Entity, IImmutableRecord
{
    public Guid VisitId { get; private set; }
    public Visit Visit { get; private set; } = null!;
    public Guid MaterialId { get; private set; }
    public Material Material { get; private set; } = null!;
    public decimal QuantityUsed { get; private set; }
    public DateTimeOffset UsedAt { get; private set; }

    private MaterialUsage()
    {
    }

    public static MaterialUsage Create(Guid visitId, Guid materialId, decimal quantityUsed)
    {
        if (quantityUsed <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityUsed), "Количество списания должно быть больше нуля.");

        return new MaterialUsage
        {
            VisitId = visitId,
            MaterialId = materialId,
            QuantityUsed = quantityUsed,
            UsedAt = DateTimeOffset.UtcNow
        };
    }
}
