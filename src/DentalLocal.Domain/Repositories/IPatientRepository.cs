using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> PhoneExistsAsync(string normalizedPhone, Guid? excludePatientId = null, CancellationToken cancellationToken = default);
}
