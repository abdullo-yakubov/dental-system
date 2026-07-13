using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IPatientFileRepository : IRepository<PatientFile>
{
    Task<IReadOnlyList<PatientFile>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<bool> IsAttachedToVisitAsync(Guid fileId, CancellationToken cancellationToken = default);

    void Remove(PatientFile file);
}
