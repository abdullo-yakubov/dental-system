using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class TreatmentPlanItem : Entity
{
    public Guid TreatmentPlanId { get; private set; }
    public TreatmentPlan TreatmentPlan { get; private set; } = null!;
    public Guid ProcedureId { get; private set; }
    public Procedure Procedure { get; private set; } = null!;

    /// <summary>Номер зуба по FDI либо null, если позиция плана не привязана к зубу.</summary>
    public short? ToothNumber { get; private set; }

    public decimal EstimatedPrice { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsCompleted { get; private set; }
    public Guid? CompletedVisitId { get; private set; }

    private TreatmentPlanItem()
    {
    }

    internal static TreatmentPlanItem Create(
        Guid treatmentPlanId, Guid procedureId, short? toothNumber, decimal estimatedPrice, int orderIndex)
    {
        if (toothNumber is not null && !TreatmentItem.IsValidToothNumber(toothNumber.Value))
            throw new ArgumentOutOfRangeException(nameof(toothNumber), "Некорректный номер зуба по FDI.");

        if (estimatedPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedPrice), "Цена не может быть отрицательной.");

        return new TreatmentPlanItem
        {
            TreatmentPlanId = treatmentPlanId,
            ProcedureId = procedureId,
            ToothNumber = toothNumber,
            EstimatedPrice = estimatedPrice,
            OrderIndex = orderIndex,
        };
    }

    internal void MarkCompleted(Guid completedVisitId)
    {
        if (IsCompleted)
            throw new InvalidOperationException("Позиция плана лечения уже отмечена выполненной.");

        IsCompleted = true;
        CompletedVisitId = completedVisitId;
        Touch();
    }
}
