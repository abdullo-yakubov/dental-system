using DentalLocal.Application.Common;
using DentalLocal.Domain.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
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
            (AppSettingKeys.Enable3DOdontogram, "false"),
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

    private const string TestDataSource = "Тестовые данные";
    private const int TestPatientCount = 100;

    private static readonly string[] LastNameStems =
    [
        "Иванов", "Петров", "Сидоров", "Кузнецов", "Смирнов", "Попов", "Соколов", "Лебедев",
        "Козлов", "Новиков", "Морозов", "Волков", "Алексеев", "Семёнов", "Егоров", "Павлов",
        "Степанов", "Николаев", "Орлов", "Андреев", "Макаров", "Никитин", "Захаров", "Соловьёв",
        "Борисов", "Яковлев", "Григорьев", "Романов", "Воробьёв", "Сергеев",
    ];

    private static readonly string[] MaleFirstNames =
    [
        "Александр", "Дмитрий", "Максим", "Сергей", "Андрей", "Алексей", "Артём", "Илья",
        "Кирилл", "Никита", "Михаил", "Егор", "Матвей", "Иван", "Роман", "Владимир",
        "Денис", "Евгений", "Олег", "Виктор",
    ];

    private static readonly string[] FemaleFirstNames =
    [
        "Анна", "Мария", "Елена", "Ольга", "Наталья", "Татьяна", "Ирина", "Светлана",
        "Юлия", "Екатерина", "Анастасия", "Виктория", "Дарья", "Полина", "Валентина",
        "Людмила", "Марина", "Кристина", "Вероника", "Алина",
    ];

    private static readonly (string Male, string Female)[] Patronymics =
    [
        ("Александрович", "Александровна"), ("Сергеевич", "Сергеевна"), ("Иванович", "Ивановна"),
        ("Петрович", "Петровна"), ("Николаевич", "Николаевна"), ("Викторович", "Викторовна"),
        ("Андреевич", "Андреевна"), ("Дмитриевич", "Дмитриевна"), ("Олегович", "Олеговна"),
        ("Игоревич", "Игоревна"),
    ];

    private static readonly string[] Streets =
    [
        "ул. Рудаки", "ул. Айни", "ул. Сомони", "ул. Фирдавси", "ул. Дружбы Народов",
        "пр. Исмоила Сомони", "ул. Борбад", "ул. Носира Хусрава", "ул. Мирзо Турсунзаде", "ул. Бехзод",
    ];

    private static readonly string[] DoctorNames =
        ["Рахимов А.А.", "Каримова Д.С.", "Назаров И.Б.", "Юсупова М.Н.", "Холов Ш.Т."];

    private static readonly string[] AllergySamples = ["Пенициллин", "Лидокаин", "Латекс", "Аспирин"];

    private static readonly string[] ChronicDiseaseSamples =
        ["Гипертония", "Сахарный диабет 2 типа", "Астма", "Гастрит"];

    /// <summary>
    /// Наполняет базу 100 разнообразными тестовыми пациентами (со случайными визитами и оплатами),
    /// чтобы вручную проверить работу всех разделов на непустых/разнородных данных. Вызывается только
    /// в Development-окружении (см. MigrationExtensions.MigrateDatabaseAsync) — в проде клиники это
    /// было бы мусором в реальной базе. Идемпотентно: считает уже созданных ранее тестовых пациентов
    /// по маркеру <see cref="TestDataSource"/> и доращивает их количество до <see cref="TestPatientCount"/>.
    /// </summary>
    public async Task SeedTestPatientsAsync(CancellationToken cancellationToken = default)
    {
        var existingCount = await context.Patients.CountAsync(p => p.Source == TestDataSource, cancellationToken);
        if (existingCount >= TestPatientCount)
            return;

        var procedures = await context.Procedures.Where(p => p.IsActive).ToListAsync(cancellationToken);
        if (procedures.Count == 0)
        {
            logger.LogWarning("Тестовые пациенты не сгенерированы: каталог процедур пуст");
            return;
        }

        var random = Random.Shared;
        var today = DateOnly.FromDateTime(DateTime.Now);

        for (var i = existingCount; i < TestPatientCount; i++)
        {
            var isMale = random.Next(2) == 0;
            var gender = isMale ? Gender.Male : Gender.Female;
            var firstName = isMale
                ? MaleFirstNames[random.Next(MaleFirstNames.Length)]
                : FemaleFirstNames[random.Next(FemaleFirstNames.Length)];
            var lastNameStem = LastNameStems[random.Next(LastNameStems.Length)];
            var lastName = isMale ? lastNameStem : lastNameStem + "а";
            var patronymics = Patronymics[random.Next(Patronymics.Length)];
            var middleName = isMale ? patronymics.Male : patronymics.Female;
            var birthDate = today.AddYears(-random.Next(3, 85)).AddDays(-random.Next(0, 365));
            var phone = RandomLocalPhone(random);
            var phoneExtra = random.Next(100) < 30 ? RandomLocalPhone(random) : null;
            var address = $"г. Душанбе, {Streets[random.Next(Streets.Length)]}, д. {random.Next(1, 120)}";
            var allergies = random.Next(100) < 25 ? AllergySamples[random.Next(AllergySamples.Length)] : null;
            var chronicDiseases = random.Next(100) < 20
                ? ChronicDiseaseSamples[random.Next(ChronicDiseaseSamples.Length)]
                : null;

            var patient = Patient.Create(
                lastName, firstName, middleName, gender, birthDate,
                phone, phoneExtra, address, allergies, chronicDiseases,
                comments: null, doctorNotes: null, source: TestDataSource);

            await context.Patients.AddAsync(patient, cancellationToken);

            var visitCount = random.Next(0, 5);
            for (var v = 0; v < visitCount; v++)
            {
                var visitDate = today.AddDays(random.Next(-180, 30));
                var visitTime = new TimeOnly(random.Next(9, 18), random.Next(2) == 0 ? 0 : 30);

                var visit = Visit.Create(
                    patient.Id, visitDate, visitTime,
                    DoctorNames[random.Next(DoctorNames.Length)],
                    notes: null, prescriptions: null, materialsUsed: null);

                var itemCount = random.Next(1, 4);
                for (var it = 0; it < itemCount; it++)
                {
                    var procedure = procedures[random.Next(procedures.Count)];
                    short? tooth = procedure.RequiresTooth ? RandomToothNumber(random) : null;
                    visit.AddItem(procedure.Id, tooth, procedure.Price, quantity: 1, comment: null);
                }

                if (visit.Status == VisitStatus.Completed && visit.TotalCost > 0)
                {
                    var paymentRoll = random.Next(100);
                    var paidAmount = paymentRoll switch
                    {
                        < 60 => visit.TotalCost,
                        < 85 => Math.Round(visit.TotalCost * 0.5m, 2),
                        _ => 0m,
                    };

                    if (paidAmount > 0)
                    {
                        visit.ApplyPayment(paidAmount);
                        await context.Payments.AddAsync(
                            Payment.Create(visit.Id, patient.Id, paidAmount, RandomPaymentMethod(random),
                                PaymentKind.AtTreatment, note: null),
                            cancellationToken);
                    }
                }

                patient.RegisterVisit(new DateTimeOffset(visitDate, visitTime, TimeSpan.Zero));
                await context.Visits.AddAsync(visit, cancellationToken);
            }

            if (i % 20 == 0)
                await context.SaveChangesAsync(cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Сгенерировано {Count} тестовых пациентов", TestPatientCount - existingCount);
    }

    private static string RandomLocalPhone(Random random) =>
        $"9{random.Next(0, 10)}{random.Next(1000000, 10000000)}";

    private static short RandomToothNumber(Random random)
    {
        var quadrant = new[] { 1, 2, 3, 4 }[random.Next(4)];
        var tooth = random.Next(1, 9);
        return (short)(quadrant * 10 + tooth);
    }

    private static PaymentMethod RandomPaymentMethod(Random random) =>
        (PaymentMethod)random.Next(1, 4);
}
