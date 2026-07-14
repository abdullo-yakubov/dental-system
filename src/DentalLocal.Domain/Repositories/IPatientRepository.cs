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

    /// <summary>Поиск возможных дублей по фамилии+имени и/или дате рождения (защита от задвоения карт, п.17.13 ТЗ).</summary>
    Task<IReadOnlyList<Patient>> FindPossibleDuplicatesAsync(
        string lastName, string firstName, DateOnly birthDate, CancellationToken cancellationToken = default);

    /// <summary>Пациенты без визита дольше threshold месяцев (или вообще без визитов), не в архиве.</summary>
    Task<IReadOnlyList<Patient>> GetInactiveAsync(int monthsThreshold, CancellationToken cancellationToken = default);
}
