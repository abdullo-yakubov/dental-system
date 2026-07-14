using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Visits;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Visits;

public sealed record CreateVisitCommand(
    Guid PatientId,
    DateOnly VisitDate,
    TimeOnly VisitTime,
    string DoctorName,
    string? Notes,
    string? Prescriptions,
    string? MaterialsUsed,
    IReadOnlyList<CreateVisitItemCommand> Items,
    string? Diagnosis = null) : ICommand<VisitDto>;

public sealed record CreateVisitItemCommand(
    Guid ProcedureId,
    short? ToothNumber,
    decimal Price,
    int Quantity,
    string? Comment);

public sealed class CreateVisitCommandValidator : AbstractValidator<CreateVisitCommand>
{
    public CreateVisitCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Укажите пациента.");
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200).WithMessage("Укажите имя врача.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProcedureId).NotEmpty().WithMessage("Укажите процедуру.");
            item.RuleFor(i => i.Price).GreaterThanOrEqualTo(0).WithMessage("Цена не может быть отрицательной.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Количество должно быть больше 0.");
        });
    }
}

public sealed class CreateVisitCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreateVisitCommand, Result<VisitDto>>
{
    public async Task<Result<VisitDto>> Handle(CreateVisitCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<VisitDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        Visit visit;
        var warranties = new List<Warranty>();
        var followUpReminders = new List<Reminder>();
        try
        {
            visit = Visit.Create(
                request.PatientId,
                request.VisitDate,
                request.VisitTime,
                request.DoctorName,
                request.Notes,
                request.Prescriptions,
                request.MaterialsUsed,
                request.Diagnosis);

            var earliestFollowUpDays = (int?)null;

            foreach (var item in request.Items)
            {
                var procedure = await unitOfWork.Procedures.GetByIdAsync(item.ProcedureId, cancellationToken);
                if (procedure is null)
                    return Result.Failure<VisitDto>(Error.NotFound("Procedure.NotFound", $"Процедура {item.ProcedureId} не найдена."));

                visit.AddItem(item.ProcedureId, item.ToothNumber, item.Price, item.Quantity, item.Comment);

                if (procedure.WarrantyMonths > 0)
                {
                    warranties.Add(Warranty.Create(
                        request.PatientId, visit.Id, item.ProcedureId, item.ToothNumber, request.VisitDate, procedure.WarrantyMonths, null));
                }

                // Авто-триггер контрольного визита после процедур с заданным интервалом (п.17.13 ТЗ).
                if (procedure.FollowUpDays is > 0 && (earliestFollowUpDays is null || procedure.FollowUpDays < earliestFollowUpDays))
                    earliestFollowUpDays = procedure.FollowUpDays;
            }

            if (earliestFollowUpDays is > 0)
            {
                var reminderDate = new DateTimeOffset(
                    request.VisitDate.Year, request.VisitDate.Month, request.VisitDate.Day,
                    request.VisitTime.Hour, request.VisitTime.Minute, 0, TimeSpan.Zero).AddDays(earliestFollowUpDays.Value);

                followUpReminders.Add(Reminder.Create(
                    request.PatientId, visit.Id, "Контрольный визит после лечения", reminderDate, ReminderType.FollowUp, null));
            }
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<VisitDto>(Error.Validation("Visit.InvalidData", ex.Message));
        }

        patient.RegisterVisit(new DateTimeOffset(
            request.VisitDate.Year, request.VisitDate.Month, request.VisitDate.Day,
            request.VisitTime.Hour, request.VisitTime.Minute, 0, TimeSpan.Zero));

        await unitOfWork.Visits.AddAsync(visit, cancellationToken);

        foreach (var warranty in warranties)
            await unitOfWork.Warranties.AddAsync(warranty, cancellationToken);

        foreach (var reminder in followUpReminders)
            await unitOfWork.Reminders.AddAsync(reminder, cancellationToken);

        if (visit.RemainingAmount > 0)
        {
            var debt = Debt.Create(visit.Id, visit.PatientId, visit.RemainingAmount);
            await unitOfWork.Debts.AddAsync(debt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("VISIT_CREATED", "Visit", visit.Id, null), cancellationToken);

        var createdVisit = await unitOfWork.Visits.GetByIdWithItemsAsync(visit.Id, cancellationToken);
        var dto = mapper.Map<VisitDto>(createdVisit);

        var scheduleWarning = await GetScheduleWarningAsync(request.DoctorName, request.VisitDate, request.VisitTime, cancellationToken);
        if (scheduleWarning is not null)
            dto = dto with { ScheduleWarning = scheduleWarning };

        return Result.Success(dto);
    }

    /// <summary>Предупреждение (не блокировка) при записи вне рабочего графика врача (раздел 17.8 ТЗ).
    /// Если график ещё не настроен вовсе (нет ни одной активной записи) — не предупреждаем.</summary>
    private async Task<string?> GetScheduleWarningAsync(
        string doctorName, DateOnly visitDate, TimeOnly visitTime, CancellationToken cancellationToken)
    {
        var allSchedules = await unitOfWork.DoctorSchedules.GetAllAsync(cancellationToken);
        if (!allSchedules.Any(s => s.IsActive))
            return null;

        var todaySchedule = allSchedules.FirstOrDefault(s => s.DoctorName == doctorName && s.DayOfWeek == visitDate.DayOfWeek);

        if (todaySchedule is null || !todaySchedule.IsActive)
            return "Для врача не задан рабочий график на этот день недели.";

        if (visitTime < todaySchedule.StartTime || visitTime > todaySchedule.EndTime)
            return $"Время визита ({visitTime:HH:mm}) вне рабочего графика ({todaySchedule.StartTime:HH:mm}–{todaySchedule.EndTime:HH:mm}).";

        return null;
    }
}
