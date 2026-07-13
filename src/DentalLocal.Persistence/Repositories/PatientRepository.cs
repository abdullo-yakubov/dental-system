using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace DentalLocal.Persistence.Repositories;

public class PatientRepository(DentalDbContext context) : RepositoryBase<Patient>(context), IPatientRepository
{
    public Task<Patient?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <summary>Поиск по tsvector (ФИО+телефон+адрес) с fallback на ILIKE, ускоренный триграммным GIN-индексом.</summary>
    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!includeArchived)
            query = query.Where(p => !p.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();

            if (int.TryParse(trimmed.TrimStart('К', 'к', '-'), out var cardNumber))
            {
                query = query.Where(p => p.CardNumber == cardNumber);
            }
            else
            {
                var likePattern = $"%{trimmed}%";

                query = query.Where(p =>
                    EF.Property<NpgsqlTsVector>(p, "SearchVector").Matches(EF.Functions.PlainToTsQuery("russian", trimmed)) ||
                    EF.Functions.ILike(p.Phone, likePattern) ||
                    EF.Functions.ILike(p.LastName, likePattern) ||
                    EF.Functions.ILike(p.FirstName, likePattern));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> PhoneExistsAsync(string normalizedPhone, Guid? excludePatientId = null, CancellationToken cancellationToken = default)
    {
        var query = Set.Where(p => p.Phone == normalizedPhone);

        if (excludePatientId is not null)
            query = query.Where(p => p.Id != excludePatientId);

        return await query.AnyAsync(cancellationToken);
    }
}
