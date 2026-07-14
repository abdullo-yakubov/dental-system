using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Reminders;
using DentalLocal.Shared.Enums;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Reminders;

/// <summary>Кнопка «Напомнить» на карточке визита (см. п. 19.3 ТЗ).</summary>
public sealed record CreateReminderCommand(
    Guid PatientId,
    Guid? VisitId,
    string Title,
    DateTimeOffset ReminderDate,
    ReminderType Type,
    string? Notes) : ICommand<ReminderDto>;

public sealed class CreateReminderCommandValidator : AbstractValidator<CreateReminderCommand>
{
    public CreateReminderCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255).WithMessage("Укажите название напоминания.");
    }
}

public sealed class CreateReminderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateReminderCommand, Result<ReminderDto>>
{
    public async Task<Result<ReminderDto>> Handle(CreateReminderCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<ReminderDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var reminder = Reminder.Create(
            request.PatientId,
            request.VisitId,
            request.Title,
            request.ReminderDate,
            (Domain.Enums.ReminderType)(short)request.Type,
            request.Notes);

        await unitOfWork.Reminders.AddAsync(reminder, cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("REMINDER_CREATED", "Reminder", reminder.Id, null), cancellationToken);

        return Result.Success(mapper.Map<ReminderDto>(reminder));
    }
}
