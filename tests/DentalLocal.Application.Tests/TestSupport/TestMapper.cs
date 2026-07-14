using AutoMapper;
using DentalLocal.Application.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLocal.Application.Tests.TestSupport;

// AutoMapper 16 регистрируется только через IServiceCollection.AddAutoMapper — прямой
// конструктор MapperConfiguration(Action<...>) в продакшн-коде тоже не используется
// (см. Application/DependencyInjection.cs), поэтому тестовый IMapper собирается так же.
internal static class TestMapper
{
    public static IMapper Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<FinancialMappingProfile>());
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
