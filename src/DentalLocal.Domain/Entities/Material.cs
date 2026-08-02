using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class Material : Entity
{
    public string Name { get; private set; } = null!;
    public string Unit { get; private set; } = null!;
    public decimal QuantityInStock { get; private set; }
    public decimal MinStockThreshold { get; private set; }
    public decimal PricePerUnit { get; private set; }

    /// <summary>Concurrency token, отображается на системную колонку Postgres "xmin" (см. MaterialConfiguration).</summary>
    public uint Version { get; private set; }

    private Material()
    {
    }

    public static Material Create(string name, string unit, decimal quantityInStock, decimal minStockThreshold, decimal pricePerUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название материала обязательно.", nameof(name));
        
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Единица измерения обязательна.", nameof(unit));

        if (quantityInStock < 0)
            throw new ArgumentOutOfRangeException(nameof(quantityInStock), "Количество не может быть отрицательным.");

        if (minStockThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(minStockThreshold), "Порог остатка не может быть отрицательным.");

        if (pricePerUnit < 0)
            throw new ArgumentOutOfRangeException(nameof(pricePerUnit), "Цена за единицу не может быть отрицательной.");

        return new Material
        {
            Name = name.Trim(),
            Unit = unit.Trim(),
            QuantityInStock = quantityInStock,
            MinStockThreshold = minStockThreshold,
            PricePerUnit = pricePerUnit
        };
    }

    public void Update(string name, string unit, decimal minStockThreshold, decimal pricePerUnit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название материала обязательно.", nameof(name));
        
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Единица измерения обязательна.", nameof(unit));

        Name = name.Trim();
        Unit = unit.Trim();
        MinStockThreshold = minStockThreshold;
        PricePerUnit = pricePerUnit;
        Touch();
    }

    public void AdjustStock(decimal amount)
    {
        if (QuantityInStock + amount < 0)
            throw new InvalidOperationException("Недостаточно материала на складе.");

        QuantityInStock += amount;
        Touch();
    }

    /// <summary>
    /// Списание материала при использовании в визите — в отличие от AdjustStock, НЕ блокирует
    /// уход остатка в минус (по ТЗ уход в минус — предупреждение для склада, а не повод отменить
    /// уже проведённое лечение). Возвращает true, если остаток после списания отрицательный —
    /// вызывающая сторона решает, как это показать пользователю.
    /// </summary>
    public bool DeductForUsage(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Количество списания должно быть больше нуля.");

        QuantityInStock -= quantity;
        Touch();

        return QuantityInStock < 0;
    }
}
