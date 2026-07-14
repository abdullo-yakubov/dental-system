using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Application.Tests.TestSupport;

// AutoMapper 16 регистрируется только через IServiceCollection.AddAutoMapper — прямой
// конструктор MapperConfiguration(Action<...>) в продакшн-коде тоже не используется
// (см. Application/DependencyInjection.cs). Регистрируем все профили из сборки Application,
// как в реальном DI, чтобы тестам не нужно было знать, какой конкретно профиль им нужен.
internal static class TestMapper
{
    public static IMapper Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.Load("DentalLocal.Application")));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
