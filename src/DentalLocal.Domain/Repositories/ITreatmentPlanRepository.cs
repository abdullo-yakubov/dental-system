using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface ITreatmentPlanRepository : IRepository<TreatmentPlan>
{
    Task<TreatmentPlan?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TreatmentPlan>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}
