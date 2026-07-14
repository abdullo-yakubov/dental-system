namespace DentalLocal.Shared.Contracts.DoctorSchedule;

public sealed record DoctorScheduleDto(
    Guid Id,
    string DoctorName,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

public sealed record UpsertDoctorScheduleRequest(
    string DoctorName,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);
