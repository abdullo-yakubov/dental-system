using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class VisitRepository(DentalDbContext context) : RepositoryBase<Visit>(context), IVisitRepository
{
    public Task<Visit?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(v => v.Items).ThenInclude(i => i.Procedure)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Visit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(v => v.Items).ThenInclude(i => i.Procedure)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.VisitTime)
            .ToListAsync(cancellationToken);
}
