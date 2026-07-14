using DentalLocal.Domain.Repositories;
using DentalLocal.Persistence.Repositories;
using DentalLocal.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DentalDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DentalDbContext.Schema))
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
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IDiscountHistoryRepository, DiscountHistoryRepository>();
        services.AddScoped<IWaitingListRepository, WaitingListRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IWarrantyRepository, WarrantyRepository>();
        services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DentalLocal.Application.Interfaces.IDashboardService, Services.DashboardService>();

        services.AddScoped<DbSeeder>();

        return services;
    }
}
