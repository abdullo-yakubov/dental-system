using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Tests;

public class MaterialTests
{
    private static Material CreateMaterial(decimal quantityInStock = 10m, decimal pricePerUnit = 5_000m) =>
        Material.Create("Анестетик", "мл", quantityInStock, minStockThreshold: 2m, pricePerUnit);

    [Fact]
    public void AdjustStock_BelowZero_Throws()
    {
        var material = CreateMaterial(quantityInStock: 5m);

        Assert.Throws<InvalidOperationException>(() => material.AdjustStock(-10m));
    }

    [Fact]
    public void DeductForUsage_ExceedsStock_DoesNotThrow_AndReturnsTrue()
    {
        // По ТЗ уход остатка в минус при списании по визиту — предупреждение, а не блокировка
        // (в отличие от AdjustStock, который используется для ручной коррекции склада).
        var material = CreateMaterial(quantityInStock: 5m);

        var wentNegative = material.DeductForUsage(8m);

        Assert.True(wentNegative);
        Assert.Equal(-3m, material.QuantityInStock);
    }

    [Fact]
    public void DeductForUsage_WithinStock_ReturnsFalse()
    {
        var material = CreateMaterial(quantityInStock: 10m);

        var wentNegative = material.DeductForUsage(4m);

        Assert.False(wentNegative);
        Assert.Equal(6m, material.QuantityInStock);
    }

    [Fact]
    public void DeductForUsage_NonPositiveQuantity_Throws()
    {
        var material = CreateMaterial();

        Assert.Throws<ArgumentOutOfRangeException>(() => material.DeductForUsage(0m));
    }
}

public class MaterialUsageTests
{
    [Fact]
    public void Create_CapturesPricePerUnitSnapshot()
    {
        var usage = MaterialUsage.Create(Guid.NewGuid(), Guid.NewGuid(), quantityUsed: 2m, pricePerUnitSnapshot: 15_000m);

        Assert.Equal(15_000m, usage.PricePerUnitSnapshot);
    }

    [Fact]
    public void Create_NonPositiveQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MaterialUsage.Create(Guid.NewGuid(), Guid.NewGuid(), quantityUsed: 0m, pricePerUnitSnapshot: 1_000m));
    }
}
