using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IWaitingListRepository : IRepository<WaitingList>
{
    Task<IReadOnlyList<WaitingList>> GetAllAsync(string? status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitingList>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
}
