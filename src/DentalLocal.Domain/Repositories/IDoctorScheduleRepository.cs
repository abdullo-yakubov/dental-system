using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IDoctorScheduleRepository : IRepository<DoctorSchedule>
{
    Task<IReadOnlyList<DoctorSchedule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DoctorSchedule?> GetByDayAsync(string doctorName, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
}
