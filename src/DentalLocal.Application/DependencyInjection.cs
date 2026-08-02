using System.Reflection;
using DentalLocal.Application.Behaviors;
using DentalLocal.Application.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        services.AddSingleton<ILoginAttemptTracker, InMemoryLoginAttemptTracker>();
        services.AddMemoryCache();

        // Порядок регистрации = порядок вложенности (первый — самый внешний):
        // Logging -> Performance -> Validation -> Transaction -> AuditLogging -> Handler.
        // AuditLogging — самый внутренний из behavior'ов: должен выполниться до SaveChangesAsync,
        // который вызывает TransactionBehavior, чтобы запись аудита попала в ту же транзакцию.
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(AuditLoggingBehavior<,>));

        return services;
    }
}
