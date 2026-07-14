using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

public class TreatmentPlan : Entity
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Notes { get; private set; }
    public TreatmentPlanStatus Status { get; private set; }

    private readonly List<TreatmentPlanItem> _items = [];
    public IReadOnlyCollection<TreatmentPlanItem> Items => _items.AsReadOnly();

    private TreatmentPlan()
    {
    }

    public static TreatmentPlan Create(Guid patientId, string title, string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название плана лечения обязательно.", nameof(title));

        return new TreatmentPlan
        {
            PatientId = patientId,
            Title = title.Trim(),
            Notes = notes,
            Status = TreatmentPlanStatus.Draft,
        };
    }

    public void Update(string title, string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название плана лечения обязательно.", nameof(title));

        Title = title.Trim();
        Notes = notes;
        Touch();
    }

    public TreatmentPlanItem AddItem(Guid procedureId, short? toothNumber, decimal estimatedPrice, int orderIndex)
    {
        if (Status is TreatmentPlanStatus.Completed or TreatmentPlanStatus.Cancelled)
            throw new InvalidOperationException("Нельзя добавить позицию в завершённый или отменённый план лечения.");

        var item = TreatmentPlanItem.Create(Id, procedureId, toothNumber, estimatedPrice, orderIndex);
        _items.Add(item);
        Touch();
        return item;
    }

    public void Activate()
    {
        if (Status != TreatmentPlanStatus.Draft)
            throw new InvalidOperationException("Активировать можно только черновик плана лечения.");

        Status = TreatmentPlanStatus.Active;
        Touch();
    }

    public void Cancel()
    {
        if (Status == TreatmentPlanStatus.Completed)
            throw new InvalidOperationException("Нельзя отменить завершённый план лечения.");

        Status = TreatmentPlanStatus.Cancelled;
        Touch();
    }

    public void CompleteItem(Guid itemId, Guid completedVisitId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Позиция плана лечения не найдена.");

        item.MarkCompleted(completedVisitId);
        Touch();

        if (_items.Count > 0 && _items.All(i => i.IsCompleted))
            Status = TreatmentPlanStatus.Completed;
    }

    public decimal TotalEstimatedPrice => _items.Sum(i => i.EstimatedPrice);
    public int CompletedCount => _items.Count(i => i.IsCompleted);
}
