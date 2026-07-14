using AutoMapper;
using DentalLocal.Application.Features.Reminders;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;
using DomainEnums = DentalLocal.Domain.Enums;
using SharedEnums = DentalLocal.Shared.Enums;

namespace DentalLocal.Application.Tests.Features.Reminders;

public class CreateReminderCommandHandlerTests
{
    private static IMapper CreateMapper() => TestMapper.Create();

    private static Patient CreatePatient() =>
        Patient.Create("Тестов", "Тест", null, DomainEnums.Gender.Male, new DateOnly(1990, 1, 1),
            "901234567", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_ValidRequest_CreatesReminderWithRequestedType()
    {
        var uow = new FakeUnitOfWork();
        var patient = CreatePatient();
        uow.PatientsData.Add(patient);

        var handler = new CreateReminderCommandHandler(uow, CreateMapper());
        var reminderDate = DateTimeOffset.UtcNow.AddDays(7);
        var command = new CreateReminderCommand(
            patient.Id, null, "Контроль после лечения каналов", reminderDate, SharedEnums.ReminderType.FollowUp, "проверить пломбу");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reminder = Assert.Single(uow.RemindersData);
        Assert.Equal(DomainEnums.ReminderType.FollowUp, reminder.Type);
        Assert.False(reminder.IsCompleted);
        Assert.Equal(reminderDate, reminder.ReminderDate);
    }

    [Fact]
    public async Task Handle_PatientNotFound_ReturnsNotFoundAndDoesNotCreateReminder()
    {
        var uow = new FakeUnitOfWork();
        var handler = new CreateReminderCommandHandler(uow, CreateMapper());
        var command = new CreateReminderCommand(
            Guid.NewGuid(), null, "Напоминание", DateTimeOffset.UtcNow.AddDays(1), SharedEnums.ReminderType.Custom, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Patient.NotFound", result.Error.Code);
        Assert.Empty(uow.RemindersData);
    }
}
