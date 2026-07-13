using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class AppointmentRepository(DentalDbContext context) : RepositoryBase<Appointment>(context), IAppointmentRepository
{
    public async Task<IReadOnlyList<Appointment>> GetByRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Procedure)
            .Where(a => a.StartsAt >= from && a.StartsAt <= to)
            .OrderBy(a => a.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasConflictAsync(
        DateTimeOffset startsAt,
        int durationMinutes,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var endsAt = startsAt.AddMinutes(durationMinutes);

        var query = Set.Where(a =>
            a.Status == Domain.Enums.AppointmentStatus.Planned &&
            a.StartsAt < endsAt &&
            a.StartsAt.AddMinutes(a.DurationMinutes) > startsAt);

        if (excludeAppointmentId is not null)
            query = query.Where(a => a.Id != excludeAppointmentId);

        return await query.AnyAsync(cancellationToken);
    }
}
