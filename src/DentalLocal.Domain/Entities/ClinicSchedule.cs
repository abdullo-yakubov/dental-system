using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

/// <summary>Часы работы клиники по дням недели (в клинике один специалист — расписание единое, без привязки к врачу).</summary>
public class ClinicSchedule : Entity
{
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }

    private ClinicSchedule()
    {
    }

    public static ClinicSchedule Create(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isActive = true)
    {
        if (endTime <= startTime)
            throw new ArgumentException("Время окончания должно быть позже времени начала.");

        return new ClinicSchedule
        {
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = isActive
        };
    }

    public void Update(TimeOnly startTime, TimeOnly endTime, bool isActive)
    {
        if (endTime <= startTime)
            throw new ArgumentException("Время окончания должно быть позже времени начала.");

        StartTime = startTime;
        EndTime = endTime;
        IsActive = isActive;
        Touch();
    }
}
