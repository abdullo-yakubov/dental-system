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

    // Нечёткое совпадение по ФИО (pg_trgm similarity > 0.6, см. промпт 41 аудита) вместо точного
    // ILike — опечатка в фамилии/имени ("Карим*о*в" вместо "Карим*а*в") раньше не давала
    // предупреждения о дубле. Комбинируется через ИЛИ с совпадением даты рождения — оба сигнала
    // сами по себе достаточно редки, чтобы не давать много ложных срабатываний по отдельности.
    public async Task<IReadOnlyList<Patient>> FindPossibleDuplicatesAsync(
        string lastName, string firstName, DateOnly birthDate, CancellationToken cancellationToken = default)
    {
        var fullName = $"{lastName} {firstName}";

        return await Set.AsNoTracking()
            .Where(p => !p.IsArchived &&
                        (EF.Functions.TrigramsSimilarity(p.LastName + " " + p.FirstName, fullName) > 0.6 ||
                         p.BirthDate == birthDate))
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> GetInactiveAsync(int monthsThreshold, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-monthsThreshold);

        return await Set.AsNoTracking()
            .Where(p => !p.IsArchived && (p.LastVisitAt == null || p.LastVisitAt < cutoff))
            .OrderBy(p => p.LastVisitAt ?? DateTimeOffset.MinValue)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteWithRelatedDataAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var db = Context.Database;

        // material_usages удаляются автоматически (ON DELETE CASCADE от visits) при удалении визитов ниже.
        await db.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dental.tooth_surface_history WHERE tooth_id IN (SELECT id FROM dental.tooth_states WHERE patient_id = {patientId})",
            cancellationToken);
        await db.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dental.prescriptions WHERE visit_id IN (SELECT id FROM dental.visits WHERE patient_id = {patientId})",
            cancellationToken);
        await db.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dental.treatment_items WHERE visit_id IN (SELECT id FROM dental.visits WHERE patient_id = {patientId})",
            cancellationToken);
        await db.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM dental.treatment_plan_items WHERE treatment_plan_id IN (SELECT id FROM dental.treatment_plans WHERE patient_id = {patientId})",
            cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.tooth_state_history WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.tooth_states WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.reminders WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.warranties WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.debts WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.appointments WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.payments WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.treatment_plans WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.waiting_list WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.discount_history WHERE patient_id = {patientId}", cancellationToken);
        // Удаление patient_files выставит patients.photo_file_id в NULL (ON DELETE SET NULL) до удаления самого пациента.
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.patient_files WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.visits WHERE patient_id = {patientId}", cancellationToken);
        await db.ExecuteSqlInterpolatedAsync($"DELETE FROM dental.patients WHERE id = {patientId}", cancellationToken);
    }
}
