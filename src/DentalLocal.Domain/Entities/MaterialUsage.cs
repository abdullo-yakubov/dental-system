using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class MaterialUsage : Entity, IImmutableRecord
{
    public Guid VisitId { get; private set; }
    public Visit Visit { get; private set; } = null!;
    public Guid MaterialId { get; private set; }
    public Material Material { get; private set; } = null!;
    public decimal QuantityUsed { get; private set; }

    /// <summary>Цена материала за единицу НА МОМЕНТ списания — себестоимость визита считается по
    /// этому снапшоту, а не по текущей Material.PricePerUnit (иначе изменение цены материала задним
    /// числом переписывает историческую прибыль по уже закрытым визитам).</summary>
    public decimal PricePerUnitSnapshot { get; private set; }

    public DateTimeOffset UsedAt { get; private set; }

    private MaterialUsage()
    {
    }

    public static MaterialUsage Create(Guid visitId, Guid materialId, decimal quantityUsed, decimal pricePerUnitSnapshot)
    {
        if (quantityUsed <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantityUsed), "Количество списания должно быть больше нуля.");

        if (pricePerUnitSnapshot < 0)
            throw new ArgumentOutOfRangeException(nameof(pricePerUnitSnapshot), "Цена за единицу не может быть отрицательной.");

        return new MaterialUsage
        {
            VisitId = visitId,
            MaterialId = materialId,
            QuantityUsed = quantityUsed,
            PricePerUnitSnapshot = pricePerUnitSnapshot,
            UsedAt = DateTimeOffset.UtcNow
        };
    }
}
