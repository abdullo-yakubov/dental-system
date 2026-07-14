using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IDiscountHistoryRepository : IRepository<DiscountHistory>
{
    Task<IReadOnlyList<DiscountHistory>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}
