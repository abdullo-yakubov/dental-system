using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class DoctorScheduleRepository(DentalDbContext context) : RepositoryBase<DoctorSchedule>(context), IDoctorScheduleRepository
{
    public async Task<IReadOnlyList<DoctorSchedule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync(cancellationToken);

    public Task<DoctorSchedule?> GetByDayAsync(string doctorName, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(s => s.DoctorName == doctorName && s.DayOfWeek == dayOfWeek, cancellationToken);
}
