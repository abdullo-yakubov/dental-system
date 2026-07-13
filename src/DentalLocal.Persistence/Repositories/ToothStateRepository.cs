using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class ToothStateRepository(DentalDbContext context) : RepositoryBase<ToothState>(context), IToothStateRepository
{
    public async Task<IReadOnlyList<ToothState>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.Where(t => t.PatientId == patientId).ToListAsync(cancellationToken);

    public Task<ToothState?> GetAsync(Guid patientId, short toothNumber, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(t => t.PatientId == patientId && t.ToothNumber == toothNumber, cancellationToken);
}
