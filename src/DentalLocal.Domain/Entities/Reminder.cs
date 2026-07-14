using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

/// <summary>Напоминание/контрольный визит для пациента (см. п. 19.3, 4.15 ТЗ — кнопка «Напомнить» на карточке визита).</summary>
public class Reminder : Entity
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public Guid? VisitId { get; private set; }
    public string Title { get; private set; } = null!;
    public DateTimeOffset ReminderDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public ReminderType Type { get; private set; }
    public string? Notes { get; private set; }

    private Reminder()
    {
    }

    public static Reminder Create(
        Guid patientId,
        Guid? visitId,
        string title,
        DateTimeOffset reminderDate,
        ReminderType type,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название напоминания обязательно.", nameof(title));

        return new Reminder
        {
            PatientId = patientId,
            VisitId = visitId,
            Title = title.Trim(),
            ReminderDate = reminderDate,
            Type = type,
            Notes = notes,
            IsCompleted = false,
        };
    }

    public void Complete()
    {
        IsCompleted = true;
        Touch();
    }
}
