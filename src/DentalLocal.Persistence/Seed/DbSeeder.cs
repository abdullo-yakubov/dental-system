using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalLocal.Persistence.Seed;

/// <summary>Идемпотентный сидинг: категории процедур, каталог процедур, настройки по умолчанию (см. п.10 ТЗ).</summary>
public class DbSeeder(DentalDbContext context, ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedProcedureCategoriesAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await SeedProceduresAsync(cancellationToken);
        await SeedAppSettingsAsync(cancellationToken);
        await SeedIcdCodesAsync(cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Сидинг базы данных завершён");
    }

    private async Task SeedProcedureCategoriesAsync(CancellationToken cancellationToken)
    {
        var existingNames = await context.ProcedureCategories.Select(c => c.Name).ToListAsync(cancellationToken);

        foreach (var (name, color, sortOrder) in ProcedureCategorySeedData.Categories)
        {
            if (existingNames.Contains(name))
                continue;

            await context.ProcedureCategories.AddAsync(ProcedureCategory.Create(name, color, sortOrder), cancellationToken);
        }
    }

    private async Task SeedProceduresAsync(CancellationToken cancellationToken)
    {
        if (await context.Procedures.AnyAsync(cancellationToken))
            return;

        var categories = await context.ProcedureCategories.ToDictionaryAsync(c => c.Name, cancellationToken);

        foreach (var (categoryName, name, priceMin, priceMax, durationMinutes, requiresTooth) in ProcedureSeedData.Procedures)
        {
            if (!categories.TryGetValue(categoryName, out var category))
            {
                logger.LogWarning("Категория «{CategoryName}» не найдена при сидинге процедуры «{ProcedureName}»", categoryName, name);
                continue;
            }

            var price = (priceMin + priceMax) / 2m;
            var specialty = ProcedureSeedData.CategoryToSpecialty.GetValueOrDefault(categoryName, categoryName);

            await context.Procedures.AddAsync(
                Procedure.Create(category.Id, name, description: null, price, durationMinutes, requiresTooth,
                    priceMin, priceMax, specialty),
                cancellationToken);
        }
    }

    private async Task SeedAppSettingsAsync(CancellationToken cancellationToken)
    {
        var existingKeys = (await context.AppSettings.Select(s => s.Key).ToListAsync(cancellationToken)).ToHashSet();

        var defaults = new (string Key, string Value)[]
        {
            (AppSettingKeys.AccessCodeHash, PasswordHasher.Hash(DentalDefaults.AccessCode)),
            (AppSettingKeys.DoctorName, "Врач-администратор"),
            (AppSettingKeys.ClinicName, "DentalLocal"),
            (AppSettingKeys.Currency, DentalDefaults.Currency),
            (AppSettingKeys.Theme, "system"),
            (AppSettingKeys.BackupTime, DentalDefaults.BackupTime),
            (AppSettingKeys.BackupRetentionDays, DentalDefaults.BackupRetentionDays.ToString()),
            (AppSettingKeys.Language, "ru"),
        };

        foreach (var (key, value) in defaults)
        {
            if (existingKeys.Contains(key))
                continue;

            await context.AppSettings.AddAsync(AppSetting.Create(key, value), cancellationToken);
        }
    }

    private async Task SeedIcdCodesAsync(CancellationToken cancellationToken)
    {
        if (!await context.Icd10Codes.AnyAsync(cancellationToken))
        {
            foreach (var (code, description) in Icd10SeedData.Codes)
                await context.Icd10Codes.AddAsync(Icd10Code.Create(code, description), cancellationToken);
        }

        if (!await context.Icd11Codes.AnyAsync(cancellationToken))
        {
            foreach (var (code, description) in Icd11SeedData.Codes)
                await context.Icd11Codes.AddAsync(Icd11Code.Create(code, description), cancellationToken);
        }
    }
}
