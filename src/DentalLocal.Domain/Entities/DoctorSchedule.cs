using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

/// <summary>График работы конкретного врача по дню недели (в отличие от <see cref="ClinicSchedule"/> — общих часов работы клиники).</summary>
public class DoctorSchedule : Entity
{
    public string DoctorName { get; private set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }

    private DoctorSchedule()
    {
    }

    public static DoctorSchedule Create(string doctorName, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(doctorName))
            throw new ArgumentException("Укажите врача.");

        if (endTime <= startTime)
            throw new ArgumentException("Время окончания должно быть позже времени начала.");

        return new DoctorSchedule
        {
            DoctorName = doctorName,
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
