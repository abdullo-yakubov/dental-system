using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class ClinicScheduleRepository(DentalDbContext context) : RepositoryBase<ClinicSchedule>(context), IClinicScheduleRepository
{
    public async Task<IReadOnlyList<ClinicSchedule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync(cancellationToken);

    public Task<ClinicSchedule?> GetByDayAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(s => s.DayOfWeek == dayOfWeek, cancellationToken);
}
