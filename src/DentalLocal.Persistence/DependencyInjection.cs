using DentalLocal.Domain.Repositories;
using DentalLocal.Persistence.Repositories;
using DentalLocal.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DentalLocal.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        var effectiveConnectionString = ApplyTimeouts(connectionString);

        services.AddDbContext<DentalDbContext>(options =>
            options
                .UseNpgsql(effectiveConnectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DentalDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IProcedureCategoryRepository, ProcedureCategoryRepository>();
        services.AddScoped<IProcedureRepository, ProcedureRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<IToothStateRepository, ToothStateRepository>();
        services.AddScoped<IToothStateHistoryRepository, ToothStateHistoryRepository>();
        services.AddScoped<IToothSurfaceHistoryRepository, ToothSurfaceHistoryRepository>();
        services.AddScoped<IIcd10CodeRepository, Icd10CodeRepository>();
        services.AddScoped<IIcd11CodeRepository, Icd11CodeRepository>();
        services.AddScoped<IPatientFileRepository, PatientFileRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IBackupRecordRepository, BackupRecordRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IMaterialRepository, MaterialRepository>();
        services.AddScoped<IMaterialUsageRepository, MaterialUsageRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();
        services.AddScoped<IDiscountHistoryRepository, DiscountHistoryRepository>();
        services.AddScoped<IWaitingListRepository, WaitingListRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IWarrantyRepository, WarrantyRepository>();
        services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
        services.AddScoped<IClinicScheduleRepository, ClinicScheduleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DentalLocal.Application.Interfaces.IDashboardService, Services.DashboardService>();
        services.AddScoped<DentalLocal.Application.Common.IDatabaseMigrator, Services.DatabaseMigrator>();

        services.AddScoped<DbSeeder>();

        return services;
    }

    /// <summary>
    /// Гарантирует таймауты подключения/команд/пула и серверные session-level timeout'ы,
    /// не полагаясь на то, что appsettings/user-secrets их когда-либо зададут.
    /// </summary>
    private static string ApplyTimeouts(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 15,
            CommandTimeout = 30,
            MaxPoolSize = 20,
            Options = "-c statement_timeout=30000 -c lock_timeout=5000 -c idle_in_transaction_session_timeout=60000"
        };

        return builder.ConnectionString;
    }
}
