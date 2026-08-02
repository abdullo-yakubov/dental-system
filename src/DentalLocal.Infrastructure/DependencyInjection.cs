using DentalLocal.Application.Common;
using DentalLocal.Infrastructure.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITokenService, HmacTokenService>();
        services.AddSingleton<IPostgresBootstrapService, Database.PostgresBootstrapService>();
        services.AddSingleton<Security.IMachineSecretProvider, Security.MachineSecretProvider>();
        services.AddSingleton<IIdempotencyStore, Services.IdempotencyStore>();
        services.AddSingleton<IJobResultStore, Services.JobResultStore>();
        services.AddScoped<IBackupService, Services.BackupService>();
        services.AddScoped<IPdfService, Services.PdfService>();
        services.AddScoped<IExcelExportService, Services.ExcelExportService>();
        services.AddHostedService<BackgroundServices.DailyMaintenanceBackgroundService>();

        services.AddSingleton<BackgroundServices.JobQueue>();
        services.AddSingleton<IJobQueue>(sp => sp.GetRequiredService<BackgroundServices.JobQueue>());
        services.AddHostedService<BackgroundServices.JobRunnerBackgroundService>();

        return services;
    }
}
