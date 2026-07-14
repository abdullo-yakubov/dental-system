using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class PrescriptionRepository(DentalDbContext context)
    : RepositoryBase<Prescription>(context), IPrescriptionRepository
{
    public async Task<IReadOnlyList<Prescription>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(p => p.VisitId == visitId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task DeleteAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        Set.Remove(prescription);
        return Task.CompletedTask;
    }
}
