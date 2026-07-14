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
    public string DoctorName { get; private set; } = null!;
    public string? Notes { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Prescriptions { get; private set; }
    public string? MaterialsUsed { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal TotalPaid { get; private set; }
    public VisitStatus Status { get; private set; }

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
        if (string.IsNullOrWhiteSpace(doctorName))
            throw new ArgumentException("Имя врача обязательно.", nameof(doctorName));

        var visitMoment = new DateTimeOffset(visitDate, visitTime, TimeSpan.Zero);
        var status = visitMoment > DateTimeOffset.UtcNow ? VisitStatus.Planned : VisitStatus.Completed;

        return new Visit
        {
            PatientId = patientId,
            VisitDate = visitDate,
            VisitTime = visitTime,
            DoctorName = doctorName.Trim(),
            Notes = notes,
            Diagnosis = diagnosis,
            Prescriptions = prescriptions,
            MaterialsUsed = materialsUsed,
            Status = status,
            TotalCost = 0m,
            TotalPaid = 0m,
        };
    }

    public TreatmentItem AddItem(Guid procedureId, short? toothNumber, decimal price, int quantity, string? comment)
    {
        var item = TreatmentItem.Create(Id, procedureId, toothNumber, price, quantity, comment);
        _items.Add(item);
        RecalculateTotalCost();
        return item;
    }

    private void RecalculateTotalCost()
    {
        TotalCost = _items.Sum(i => i.Price * i.Quantity);
        Touch();
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
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма оплаты должна быть положительной.");

        var remaining = TotalCost - TotalPaid;
        if (amount > remaining)
            throw new DomainException($"Сумма оплаты ({amount}) превышает остаток по визиту ({remaining}).");

        TotalPaid += amount;
        Touch();
    }

    public decimal RemainingAmount => TotalCost - TotalPaid;
}
