using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class TreatmentPlanRepository(DentalDbContext context) : RepositoryBase<TreatmentPlan>(context), ITreatmentPlanRepository
{
    public Task<TreatmentPlan?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(t => t.Items).ThenInclude(i => i.Procedure)
            .Include(t => t.Patient)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TreatmentPlan>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(t => t.Items).ThenInclude(i => i.Procedure)
            .Where(t => t.PatientId == patientId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
}
