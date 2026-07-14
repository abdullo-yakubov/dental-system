using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Tests;

// TreatmentItem.Create — internal, поэтому проверяется через публичный Visit.AddItem,
// единственную точку входа для создания позиций лечения.
public class TreatmentItemTests
{
    private static Visit CreateVisit() =>
        Visit.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);

    [Theory]
    [InlineData(11)]
    [InlineData(18)]
    [InlineData(28)]
    [InlineData(38)]
    [InlineData(48)]
    public void AddItem_PermanentToothNumber_Accepted(short toothNumber)
    {
        var visit = CreateVisit();

        var item = visit.AddItem(Guid.NewGuid(), toothNumber, 100_000m, 1, null);

        Assert.Equal(toothNumber, item.ToothNumber);
    }

    // Регрессия: UI одонтограммы (OdontogramSvg.razor) поддерживает переключатель "Молочные зубы"
    // с номерами 51-55/61-65/71-75/81-85, но Domain ранее принимал только постоянные зубы (11-48),
    // из-за чего добавление процедуры на молочный зуб падало с ArgumentOutOfRangeException.
    [Theory]
    [InlineData(51)]
    [InlineData(55)]
    [InlineData(61)]
    [InlineData(65)]
    [InlineData(71)]
    [InlineData(75)]
    [InlineData(81)]
    [InlineData(85)]
    public void AddItem_DeciduousToothNumber_Accepted(short toothNumber)
    {
        var visit = CreateVisit();

        var item = visit.AddItem(Guid.NewGuid(), toothNumber, 100_000m, 1, null);

        Assert.Equal(toothNumber, item.ToothNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(19)]
    [InlineData(56)]
    [InlineData(66)]
    [InlineData(86)]
    [InlineData(99)]
    public void AddItem_InvalidToothNumber_Throws(short toothNumber)
    {
        var visit = CreateVisit();

        Assert.Throws<ArgumentOutOfRangeException>(() => visit.AddItem(Guid.NewGuid(), toothNumber, 100_000m, 1, null));
    }

    [Fact]
    public void AddItem_NullToothNumber_Accepted()
    {
        var visit = CreateVisit();

        var item = visit.AddItem(Guid.NewGuid(), null, 50_000m, 1, null);

        Assert.Null(item.ToothNumber);
    }

    [Fact]
    public void AddItem_NegativePrice_Throws()
    {
        var visit = CreateVisit();

        Assert.Throws<ArgumentOutOfRangeException>(() => visit.AddItem(Guid.NewGuid(), null, -1m, 1, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_NonPositiveQuantity_Throws(int quantity)
    {
        var visit = CreateVisit();

        Assert.Throws<ArgumentOutOfRangeException>(() => visit.AddItem(Guid.NewGuid(), null, 10_000m, quantity, null));
    }
}
