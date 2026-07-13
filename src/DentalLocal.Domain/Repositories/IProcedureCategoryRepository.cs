using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IProcedureCategoryRepository : IRepository<ProcedureCategory>
{
    Task<IReadOnlyList<ProcedureCategory>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
