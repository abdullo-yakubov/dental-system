using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class DiscountHistoryRepository(DentalDbContext context)
    : RepositoryBase<DiscountHistory>(context), IDiscountHistoryRepository
{
    public async Task<IReadOnlyList<DiscountHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.AppliedAt)
            .ToListAsync(cancellationToken);
}
