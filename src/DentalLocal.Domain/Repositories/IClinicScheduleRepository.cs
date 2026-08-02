using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IClinicScheduleRepository : IRepository<ClinicSchedule>
{
    Task<IReadOnlyList<ClinicSchedule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ClinicSchedule?> GetByDayAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
}
