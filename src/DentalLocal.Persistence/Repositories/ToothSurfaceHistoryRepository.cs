using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class ToothSurfaceHistoryRepository(DentalDbContext context)
    : RepositoryBase<ToothSurfaceHistory>(context), IToothSurfaceHistoryRepository
{
    public async Task<IReadOnlyList<ToothSurfaceHistory>> GetByToothIdAsync(Guid toothId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(h => h.ToothId == toothId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ToothSurfaceHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(h => h.Tooth.PatientId == patientId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
}
