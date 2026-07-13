using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Exceptions;

namespace DentalLocal.Domain.Entities;

public class Appointment : Entity
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public DateTimeOffset StartsAt { get; private set; }
    public int DurationMinutes { get; private set; }
    public Guid? ProcedureId { get; private set; }
    public Procedure? Procedure { get; private set; }
    public string? Note { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public Guid? VisitId { get; private set; }

    private Appointment()
    {
    }

    public static Appointment Create(Guid patientId, DateTimeOffset startsAt, int durationMinutes, Guid? procedureId, string? note)
    {
        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Длительность записи должна быть положительной.");

        return new Appointment
        {
            PatientId = patientId,
            StartsAt = startsAt,
            DurationMinutes = durationMinutes,
            ProcedureId = procedureId,
            Note = note,
            Status = AppointmentStatus.Planned,
        };
    }

    public void Reschedule(DateTimeOffset startsAt, int durationMinutes)
    {
        if (Status != AppointmentStatus.Planned)
            throw new DomainException("Перенести можно только запланированную запись.");

        if (durationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "Длительность записи должна быть положительной.");

        StartsAt = startsAt;
        DurationMinutes = durationMinutes;
        Touch();
    }

    public void Update(Guid? procedureId, string? note)
    {
        ProcedureId = procedureId;
        Note = note;
        Touch();
    }

    public void MarkArrived(Guid visitId)
    {
        if (Status != AppointmentStatus.Planned)
            throw new DomainException("Начать приём можно только по запланированной записи.");

        Status = AppointmentStatus.Arrived;
        VisitId = visitId;
        Touch();
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Planned)
            throw new DomainException("Отметить неявку можно только по запланированной записи.");

        Status = AppointmentStatus.NoShow;
        Touch();
    }

    public void Cancel()
    {
        if (Status != AppointmentStatus.Planned)
            throw new DomainException("Отменить можно только запланированную запись.");

        Status = AppointmentStatus.Cancelled;
        Touch();
    }
}
