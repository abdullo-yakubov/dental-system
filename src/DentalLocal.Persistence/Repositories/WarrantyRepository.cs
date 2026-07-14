using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class WarrantyRepository(DentalDbContext context)
    : RepositoryBase<Warranty>(context), IWarrantyRepository
{
    public async Task<IReadOnlyList<Warranty>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(w => w.PatientId == patientId)
            .OrderByDescending(w => w.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Warranty>> GetByToothNumberAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(w => w.PatientId == patientId && w.ToothNumber == toothNumber)
            .OrderByDescending(w => w.StartDate)
            .ToListAsync(cancellationToken);
}
