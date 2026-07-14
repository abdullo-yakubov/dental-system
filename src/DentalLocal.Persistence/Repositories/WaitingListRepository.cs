using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class WaitingListRepository(DentalDbContext context)
    : RepositoryBase<WaitingList>(context), IWaitingListRepository
{
    public async Task<IReadOnlyList<WaitingList>> GetAllAsync(string? status, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(w => w.Patient)
            .Include(w => w.Procedure)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(w => w.Status == status);

        return await query
            .OrderBy(w => w.DesiredDateFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WaitingList>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(w => w.PatientId == patientId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
}
