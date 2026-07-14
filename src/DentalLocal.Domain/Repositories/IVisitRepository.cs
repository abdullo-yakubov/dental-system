using DentalLocal.Domain.Entities;

namespace DentalLocal.Domain.Repositories;

public interface IVisitRepository : IRepository<Visit>
{
    Task<Visit?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Visit>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Visit>> GetUpcomingAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Полнотекстовый поиск по заметкам врача (Notes) и диагнозу (Diagnosis) визитов.</summary>
    Task<(IReadOnlyList<Visit> Items, int TotalCount)> SearchByNotesAsync(
        string searchText, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Все визиты (с процедурами) за период — используется для аналитики загрузки врача (раздел 17.8 ТЗ).</summary>
    Task<IReadOnlyList<Visit>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

