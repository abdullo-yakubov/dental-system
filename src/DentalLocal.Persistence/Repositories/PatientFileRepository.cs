using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class PatientFileRepository(DentalDbContext context) : RepositoryBase<PatientFile>(context), IPatientFileRepository
{
    public async Task<IReadOnlyList<PatientFile>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(f => f.PatientId == patientId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> IsAttachedToVisitAsync(Guid fileId, CancellationToken cancellationToken = default) =>
        Context.Set<Patient>().AnyAsync(p => p.PhotoFileId == fileId, cancellationToken);

    public void Remove(PatientFile file) => Set.Remove(file);
}
