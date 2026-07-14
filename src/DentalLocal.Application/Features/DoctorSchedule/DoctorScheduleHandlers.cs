using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.DoctorSchedule;
using FluentValidation;
using MediatR;
using AuditLog = DentalLocal.Domain.Entities.AuditLog;
using ScheduleEntity = DentalLocal.Domain.Entities.DoctorSchedule;

namespace DentalLocal.Application.Features.DoctorSchedule;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetDoctorScheduleQuery : IQuery<IReadOnlyList<DoctorScheduleDto>>;

public sealed class GetDoctorScheduleQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetDoctorScheduleQuery, Result<IReadOnlyList<DoctorScheduleDto>>>
{
    public async Task<Result<IReadOnlyList<DoctorScheduleDto>>> Handle(GetDoctorScheduleQuery request, CancellationToken cancellationToken)
    {
        var schedules = await unitOfWork.DoctorSchedules.GetAllAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DoctorScheduleDto>>(schedules.Select(ToDto).ToList());
    }

    internal static DoctorScheduleDto ToDto(ScheduleEntity s) =>
        new(s.Id, s.DoctorName, s.DayOfWeek, s.StartTime, s.EndTime, s.IsActive);
}

// ── Commands ─────────────────────────────────────────────────────────────────

/// <summary>Создаёт запись графика на день недели, либо обновляет существующую для того же врача/дня (upsert).</summary>
public sealed record UpsertDoctorScheduleCommand(
    string DoctorName,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive) : ICommand<DoctorScheduleDto>;

public sealed class UpsertDoctorScheduleCommandValidator : AbstractValidator<UpsertDoctorScheduleCommand>
{
    public UpsertDoctorScheduleCommandValidator()
    {
        RuleFor(x => x.DoctorName).NotEmpty().WithMessage("Укажите врача.");
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("Время окончания должно быть позже времени начала.");
    }
}

public sealed class UpsertDoctorScheduleCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertDoctorScheduleCommand, Result<DoctorScheduleDto>>
{
    public async Task<Result<DoctorScheduleDto>> Handle(UpsertDoctorScheduleCommand request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.DoctorSchedules.GetByDayAsync(request.DoctorName, request.DayOfWeek, cancellationToken);

        ScheduleEntity schedule;
        try
        {
            if (existing is not null)
            {
                existing.Update(request.StartTime, request.EndTime, request.IsActive);
                schedule = existing;
            }
            else
            {
                schedule = ScheduleEntity.Create(
                    request.DoctorName, request.DayOfWeek, request.StartTime, request.EndTime, request.IsActive);
                await unitOfWork.DoctorSchedules.AddAsync(schedule, cancellationToken);
            }
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<DoctorScheduleDto>(Error.Validation("DoctorSchedule.InvalidData", ex.Message));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("DOCTOR_SCHEDULE_UPDATED", "DoctorSchedule", schedule.Id, null), cancellationToken);

        return Result.Success(GetDoctorScheduleQueryHandler.ToDto(schedule));
    }
}
