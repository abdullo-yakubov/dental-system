using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class ToothStateHistoryRepository(DentalDbContext context)
    : RepositoryBase<ToothStateHistory>(context), IToothStateHistoryRepository
{
    public async Task<IReadOnlyList<ToothStateHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(h => h.PatientId == patientId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ToothStateHistory>> GetByToothAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(h => h.PatientId == patientId && h.ToothNumber == toothNumber)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
}
