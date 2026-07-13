using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<IReadOnlyList<Appointment>> GetByRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    Task<bool> HasConflictAsync(DateTimeOffset startsAt, int durationMinutes, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default);
}
