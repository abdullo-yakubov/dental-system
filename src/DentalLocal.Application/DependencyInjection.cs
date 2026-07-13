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

        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
