using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class TreatmentItem : Entity, IImmutableRecord
{
    public Guid VisitId { get; private set; }
    public Visit Visit { get; private set; } = null!;
    public Guid ProcedureId { get; private set; }
    public Procedure Procedure { get; private set; } = null!;

    /// <summary>Номер зуба по FDI: постоянные 11-18/21-28/31-38/41-48, молочные 51-55/61-65/71-75/81-85. Либо null, если процедура не привязана к зубу.</summary>
    public short? ToothNumber { get; private set; }

    /// <summary>Снимок цены процедуры на момент лечения — не меняется при последующем изменении каталога.</summary>
    public decimal Price { get; private set; }

    public int Quantity { get; private set; }
    public string? Comment { get; private set; }

    private static readonly HashSet<short> ValidToothNumbers =
        Enumerable.Range(11, 8).Concat(Enumerable.Range(21, 8))
            .Concat(Enumerable.Range(31, 8)).Concat(Enumerable.Range(41, 8))
            .Concat(Enumerable.Range(51, 5)).Concat(Enumerable.Range(61, 5))
            .Concat(Enumerable.Range(71, 5)).Concat(Enumerable.Range(81, 5))
            .Select(n => (short)n)
            .ToHashSet();

    private TreatmentItem()
    {
    }

    internal static TreatmentItem Create(Guid visitId, Guid procedureId, short? toothNumber, decimal price, int quantity, string? comment)
    {
        if (toothNumber is not null && !IsValidToothNumber(toothNumber.Value))
            throw new ArgumentOutOfRangeException(nameof(toothNumber), "Некорректный номер зуба по FDI.");

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Цена не может быть отрицательной.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Количество должно быть положительным.");

        return new TreatmentItem
        {
            VisitId = visitId,
            ProcedureId = procedureId,
            ToothNumber = toothNumber,
            Price = price,
            Quantity = quantity,
            Comment = comment,
        };
    }

    public static bool IsValidToothNumber(short toothNumber) => ValidToothNumbers.Contains(toothNumber);
}
