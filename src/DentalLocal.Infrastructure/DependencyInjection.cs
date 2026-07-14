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
        services.AddScoped<IBackupService, Services.BackupService>();
        services.AddScoped<IPdfService, Services.PdfService>();
        services.AddHostedService<BackgroundServices.DailyMaintenanceBackgroundService>();

        return services;
    }
}
