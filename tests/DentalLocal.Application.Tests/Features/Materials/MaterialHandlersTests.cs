using AutoMapper;
using DentalLocal.Application.Features.Materials;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;

namespace DentalLocal.Application.Tests.Features.Materials;

public class AdjustMaterialStockCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    [Fact]
    public async Task Handle_PositiveAdjustment_IncreasesStock()
    {
        var uow = new FakeUnitOfWork();
        var material = Material.Create("Артикаин", "карпула", 10, 5, 15_000m);
        uow.MaterialsData.Add(material);

        var handler = new AdjustMaterialStockCommandHandler(uow, CreateMapper());
        var result = await handler.Handle(new AdjustMaterialStockCommand(material.Id, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, material.QuantityInStock);
    }

    [Fact]
    public async Task Handle_NegativeAdjustmentBelowZero_ReturnsValidationErrorAndDoesNotChangeStock()
    {
        var uow = new FakeUnitOfWork();
        var material = Material.Create("Артикаин", "карпула", 5, 5, 15_000m);
        uow.MaterialsData.Add(material);

        var handler = new AdjustMaterialStockCommandHandler(uow, CreateMapper());
        var result = await handler.Handle(new AdjustMaterialStockCommand(material.Id, -10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Material.InsufficientStock", result.Error.Code);
        Assert.Equal(5, material.QuantityInStock);
    }

    [Fact]
    public async Task Handle_MaterialNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new AdjustMaterialStockCommandHandler(uow, CreateMapper());

        var result = await handler.Handle(new AdjustMaterialStockCommand(Guid.NewGuid(), 5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Material.NotFound", result.Error.Code);
    }
}

public class RecordMaterialUsageCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    private static Visit CreateVisit() =>
        Visit.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);

    [Fact]
    public async Task Handle_SufficientStock_DeductsQuantityAndRecordsUsage()
    {
        var uow = new FakeUnitOfWork();
        var visit = CreateVisit();
        var material = Material.Create("Перчатки", "пара", 100, 20, 2_000m);
        uow.VisitsData.Add(visit);
        uow.MaterialsData.Add(material);

        var handler = new RecordMaterialUsageCommandHandler(uow, CreateMapper());
        var command = new RecordMaterialUsageCommand(visit.Id, material.Id, 4);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(96, material.QuantityInStock);
        Assert.Single(uow.MaterialUsagesData);
    }

    [Fact]
    public async Task Handle_UsageExceedsStock_SucceedsAndAllowsNegativeStock()
    {
        // По ТЗ уход остатка в минус при списании по визиту — предупреждение для склада,
        // а не повод отменить уже проведённое лечение (в отличие от ручной AdjustMaterialStockCommand).
        var uow = new FakeUnitOfWork();
        var visit = CreateVisit();
        var material = Material.Create("Перчатки", "пара", 3, 20, 2_000m);
        uow.VisitsData.Add(visit);
        uow.MaterialsData.Add(material);

        var handler = new RecordMaterialUsageCommandHandler(uow, CreateMapper());
        var command = new RecordMaterialUsageCommand(visit.Id, material.Id, 10);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(-7, material.QuantityInStock);
        Assert.Single(uow.MaterialUsagesData);
    }

    [Fact]
    public async Task Handle_RecordsUsage_CapturesPricePerUnitSnapshot()
    {
        var uow = new FakeUnitOfWork();
        var visit = CreateVisit();
        var material = Material.Create("Перчатки", "пара", 100, 20, 2_000m);
        uow.VisitsData.Add(visit);
        uow.MaterialsData.Add(material);

        var handler = new RecordMaterialUsageCommandHandler(uow, CreateMapper());
        var result = await handler.Handle(new RecordMaterialUsageCommand(visit.Id, material.Id, 4), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2_000m, uow.MaterialUsagesData.Single().PricePerUnitSnapshot);
    }
}
