using DentalLocal.Application;
using DentalLocal.Infrastructure;
using DentalLocal.Persistence;
using DentalLocal.Persistence.Seed;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace DentalLocal.Integration.Tests.TestSupport;

/// <summary>
/// Поднимает реальный Postgres в контейнере, прогоняет EF Core миграции и сидинг один раз
/// на весь прогон коллекции тестов. Каждый тест получает свой scope (см. <see cref="CreateScope"/>),
/// поэтому DbContext/change tracking не расшаривается между тестами.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private ServiceProvider? _provider;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("dental_integration")
            .WithUsername("dental")
            .WithPassword("dental")
            .Build();

        await _container.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure();
        services.AddPersistence(_container.GetConnectionString());
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DentalDbContext>();
        await context.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_provider is not null)
            await _provider.DisposeAsync();
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public IServiceScope CreateScope() => _provider!.CreateScope();

    public IMediator CreateMediator(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMediator>();
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
