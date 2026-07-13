using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IReadOnlyList<Payment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default);
}
