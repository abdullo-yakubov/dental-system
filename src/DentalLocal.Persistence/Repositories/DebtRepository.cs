using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class DebtRepository(DentalDbContext context) : RepositoryBase<Debt>(context), IDebtRepository
{
    public Task<Debt?> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(d => d.VisitId == visitId, cancellationToken);

    public async Task<IReadOnlyList<Debt>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(d => d.Patient)
            .Include(d => d.Visit)
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.AroseAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Debt> Items, int TotalCount)> SearchAsync(
        DebtStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(d => d.Patient)
            .Include(d => d.Visit)
            .AsQueryable();

        if (status is not null)
            query = query.Where(d => d.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var likePattern = $"%{search.Trim()}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.Patient.LastName, likePattern) ||
                EF.Functions.ILike(d.Patient.FirstName, likePattern) ||
                EF.Functions.ILike(d.Patient.Phone, likePattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.RemainingAmount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
