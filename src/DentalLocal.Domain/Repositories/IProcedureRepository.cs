using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IProcedureRepository : IRepository<Procedure>
{
    Task<IReadOnlyList<Procedure>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<bool> IsUsedInTreatmentAsync(Guid procedureId, CancellationToken cancellationToken = default);
}
