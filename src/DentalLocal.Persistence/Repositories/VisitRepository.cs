using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace DentalLocal.Persistence.Repositories;

public class VisitRepository(DentalDbContext context) : RepositoryBase<Visit>(context), IVisitRepository
{
    public Task<Visit?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(v => v.Items).ThenInclude(i => i.Procedure)
            .Include(v => v.Patient)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Visit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(v => v.Items).ThenInclude(i => i.Procedure)
            .Include(v => v.Patient)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.VisitTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Visit>> GetUpcomingAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Items).ThenInclude(i => i.Procedure)
            .Where(v => v.Status == VisitStatus.Planned
                        && v.VisitDate >= from
                        && v.VisitDate <= to)
            .OrderBy(v => v.VisitDate).ThenBy(v => v.VisitTime)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Visit> Items, int TotalCount)> SearchByNotesAsync(
        string searchText, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(v => v.Patient)
            .Where(v => EF.Property<NpgsqlTsVector>(v, "NotesSearchVector")
                .Matches(EF.Functions.PlainToTsQuery("russian", searchText)));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.VisitTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Visit>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(v => v.Items).ThenInclude(i => i.Procedure)
            .Where(v => v.VisitDate >= from && v.VisitDate <= to)
            .ToListAsync(cancellationToken);
}

