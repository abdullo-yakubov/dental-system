using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Exceptions;

namespace DentalLocal.Domain.Entities;

/// <summary>
/// Посещение пациента. История посещений неудаляема — доменная модель намеренно
/// не предоставляет метод удаления ни для визита, ни для его позиций лечения.
/// </summary>
public class Visit : Entity, IImmutableRecord
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public DateOnly VisitDate { get; private set; }
    public TimeOnly VisitTime { get; private set; }
    public string DoctorName { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Prescriptions { get; private set; }
    public string? MaterialsUsed { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal TotalPaid { get; private set; }
    public VisitStatus Status { get; private set; }

    /// <summary>Скидка на визит целиком (0-100%). Применяется к сумме строк ПОСЛЕ их сложения —
    /// см. RecalculateTotalCost: base = SUM(TreatmentItem.Price*Quantity) -> minus скидка визита -> TotalCost.
    /// Скидка на уровне отдельной строки (процедуры) не реализована — YAGNI для этой системы.</summary>
    public decimal DiscountPercent { get; private set; }
    public string? DiscountReason { get; private set; }

    /// <summary>Concurrency token, отображается на системную колонку Postgres "xmin" (см. VisitConfiguration).</summary>
    public uint Version { get; private set; }

    private readonly List<TreatmentItem> _items = [];
    public IReadOnlyCollection<TreatmentItem> Items => _items.AsReadOnly();

    private Visit()
    {
    }

    public static Visit Create(
        Guid patientId,
        DateOnly visitDate,
        TimeOnly visitTime,
        string doctorName,
        string? notes,
        string? prescriptions,
        string? materialsUsed,
        string? diagnosis = null)
    {
        var visitMoment = new DateTimeOffset(visitDate, visitTime, TimeSpan.Zero);
        var status = visitMoment > DateTimeOffset.UtcNow ? VisitStatus.Planned : VisitStatus.Completed;

        return new Visit
        {
            PatientId = patientId,
            VisitDate = visitDate,
            VisitTime = visitTime,
            DoctorName = doctorName,
            Notes = notes,
            Diagnosis = diagnosis,
            Prescriptions = prescriptions,
            MaterialsUsed = materialsUsed,
            Status = status,
            TotalCost = 0m,
            TotalPaid = 0m,
        };
    }

    /// <summary>
    /// Заморозка состава визита после оплаты (см. промпт 21 аудита): как только по визиту принята
    /// хотя бы частичная оплата, состав процедур больше не должен меняться — иначе врач может
    /// незаметно поменять состав уже оплаченного визита трёхмесячной давности, и вся отчётность за
    /// период поедет. До первой оплаты состав свободно редактируется (в т.ч. сразу после Visit.Create
    /// для визитов задним числом — они создаются сразу со статусом Completed, но ещё не оплачены).
    /// Отдельная проверка на Cancelled не нужна: Cancel() запрещён после оплаты (см. Cancel()), значит
    /// отменённый визит тоже всегда TotalPaid == 0.
    /// </summary>
    public TreatmentItem AddItem(Guid procedureId, short? toothNumber, decimal price, int quantity, string? comment)
    {
        if (TotalPaid > 0)
            throw new DomainException("Нельзя изменить состав визита, по которому уже есть оплата.");

        var item = TreatmentItem.Create(Id, procedureId, toothNumber, price, quantity, comment);
        _items.Add(item);
        RecalculateTotalCost();
        return item;
    }

    private void RecalculateTotalCost()
    {
        var subtotal = _items.Sum(i => i.Price * i.Quantity);
        TotalCost = subtotal - subtotal * DiscountPercent / 100m;
        Touch();
    }

    /// <summary>
    /// Задаёт скидку на визит целиком (проценты обязаны попадать в [0;100]; причина обязательна при
    /// значении больше нуля — иначе в отчётах не восстановить, почему стоимость визита занижена).
    /// Пересчитывает TotalCost немедленно.
    /// </summary>
    public void ApplyDiscount(decimal percent, string? reason)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Скидка должна быть в диапазоне 0-100%.");

        if (percent > 0 && string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Причина скидки обязательна, если скидка больше нуля.");

        if (TotalPaid > 0)
            throw new DomainException("Нельзя изменить скидку по визиту, по которому уже есть оплата.");

        DiscountPercent = percent;
        DiscountReason = percent > 0 ? reason!.Trim() : null;
        RecalculateTotalCost();
    }

    /// <summary>Только текстовые поля — финансовые поля меняются исключительно через оплаты/позиции. Диагноз не трогает — см. <see cref="SetDiagnosis"/>.</summary>
    public void UpdateNotes(string? notes, string? prescriptions, string? materialsUsed)
    {
        Notes = notes;
        Prescriptions = prescriptions;
        MaterialsUsed = materialsUsed;
        Touch();
    }

    public void SetDiagnosis(string? diagnosis)
    {
        Diagnosis = diagnosis;
        Touch();
    }

    /// <summary>Подтверждение записи врачом (см. п. 19.3 ТЗ) — переводит визит из «Запланирован» в «Подтверждён».</summary>
    public void Confirm()
    {
        if (Status != VisitStatus.Planned)
            throw new DomainException("Подтвердить можно только запланированный визит.");

        Status = VisitStatus.Confirmed;
        Touch();
    }

    public void ApplyPayment(decimal amount)
    {
        if (Status == VisitStatus.Cancelled)
            throw new DomainException("Нельзя зарегистрировать оплату по отменённому визиту.");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма оплаты должна быть положительной.");

        var remaining = TotalCost - TotalPaid;
        if (amount > remaining)
            throw new DomainException($"Сумма оплаты ({amount}) превышает остаток по визиту ({remaining}).");

        TotalPaid += amount;
        Touch();
    }

    /// <summary>
    /// Откатывает ранее применённый платёж (сторно) — симметрична ApplyPayment. Раньше это делалось
    /// рефлексией по приватному сеттеру TotalPaid в ReversePaymentCommandHandler, что позволяло
    /// увести TotalPaid в отрицательное значение без единой проверки; теперь это доменный метод
    /// с тем же уровнем защиты инвариантов, что и ApplyPayment.
    /// </summary>
    public void ReversePayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма сторно должна быть положительной.");

        if (amount > TotalPaid)
            throw new DomainException($"Сумма сторно ({amount}) превышает оплаченную сумму по визиту ({TotalPaid}).");

        TotalPaid -= amount;
        Touch();
    }

    public decimal RemainingAmount => TotalCost - TotalPaid;

    /// <summary>Перенос визита на другой слот (drag-and-drop в календаре, см. п. 7.5/21.9 ТЗ).</summary>
    public void Reschedule(DateOnly visitDate, TimeOnly visitTime)
    {
        if (Status is VisitStatus.Completed or VisitStatus.Cancelled)
            throw new DomainException("Можно перенести только запланированный или подтверждённый визит.");

        VisitDate = visitDate;
        VisitTime = visitTime;
        Touch();
    }

    /// <summary>Отмена визита (см. п. 17.2 ТЗ — история не удаляется, только помечается отменённой).</summary>
    public void Cancel()
    {
        if (Status is VisitStatus.Completed or VisitStatus.Cancelled)
            throw new DomainException("Можно отменить только запланированный или подтверждённый визит.");

        Status = VisitStatus.Cancelled;
        Touch();
    }
}
