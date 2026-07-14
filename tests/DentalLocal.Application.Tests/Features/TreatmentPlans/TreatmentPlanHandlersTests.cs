using AutoMapper;
using DentalLocal.Application.Features.TreatmentPlans;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;
using DomainEnums = DentalLocal.Domain.Enums;

namespace DentalLocal.Application.Tests.Features.TreatmentPlans;

public class AddTreatmentPlanItemCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, DomainEnums.Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    private static Procedure CreateProcedure() =>
        Procedure.Create(Guid.NewGuid(), "Пломбирование", null, 250_000m, 40, true);

    [Fact]
    public async Task Handle_ValidItem_AddsItemToPlanAndComputesTotal()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var procedure = CreateProcedure();
        var plan = TreatmentPlan.Create(patient.Id, "План лечения кариеса", null);
        uow.PatientsData.Add(patient);
        uow.ProceduresData.Add(procedure);
        uow.TreatmentPlansData.Add(plan);

        var handler = new AddTreatmentPlanItemCommandHandler(uow, CreateMapper());
        var command = new AddTreatmentPlanItemCommand(plan.Id, procedure.Id, 26, 250_000m, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(plan.Items);
        Assert.Equal(250_000m, plan.TotalEstimatedPrice);
    }

    [Fact]
    public async Task Handle_PlanNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var handler = new AddTreatmentPlanItemCommandHandler(uow, CreateMapper());
        var command = new AddTreatmentPlanItemCommand(Guid.NewGuid(), Guid.NewGuid(), null, 100_000m, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TreatmentPlan.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_PlanAlreadyCancelled_ReturnsValidationError()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        var procedure = CreateProcedure();
        var plan = TreatmentPlan.Create(patient.Id, "План", null);
        plan.Cancel();
        uow.PatientsData.Add(patient);
        uow.ProceduresData.Add(procedure);
        uow.TreatmentPlansData.Add(plan);

        var handler = new AddTreatmentPlanItemCommandHandler(uow, CreateMapper());
        var command = new AddTreatmentPlanItemCommand(plan.Id, procedure.Id, null, 100_000m, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TreatmentPlan.InvalidItem", result.Error.Code);
    }
}

public class CompleteTreatmentPlanItemCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    [Fact]
    public async Task Handle_LastItemCompleted_MarksPlanAsCompleted()
    {
        var uow = new FakeUnitOfWork();
        var patientId = Guid.NewGuid();
        var procedureId = Guid.NewGuid();
        var plan = TreatmentPlan.Create(patientId, "План", null);
        var item = plan.AddItem(procedureId, null, 150_000m, 0);
        var visit = Visit.Create(patientId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);

        uow.TreatmentPlansData.Add(plan);
        uow.VisitsData.Add(visit);

        var handler = new CompleteTreatmentPlanItemCommandHandler(uow, CreateMapper());
        var command = new CompleteTreatmentPlanItemCommand(plan.Id, item.Id, visit.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DomainEnums.TreatmentPlanStatus.Completed, plan.Status);
        Assert.Equal(1, plan.CompletedCount);
    }

    [Fact]
    public async Task Handle_VisitNotFound_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();
        var plan = TreatmentPlan.Create(Guid.NewGuid(), "План", null);
        var item = plan.AddItem(Guid.NewGuid(), null, 100_000m, 0);
        uow.TreatmentPlansData.Add(plan);

        var handler = new CompleteTreatmentPlanItemCommandHandler(uow, CreateMapper());
        var command = new CompleteTreatmentPlanItemCommand(plan.Id, item.Id, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Visit.NotFound", result.Error.Code);
    }
}
