using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Visits;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Visits;

public sealed record UpdateVisitCommand(
    Guid Id,
    DateOnly VisitDate,
    TimeOnly VisitTime,
    string DoctorName,
    string? Notes,
    string? Prescriptions,
    string? MaterialsUsed,
    string? Diagnosis = null) : ICommand<VisitDto>;

public sealed class UpdateVisitCommandValidator : AbstractValidator<UpdateVisitCommand>
{
    public UpdateVisitCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200).WithMessage("Укажите имя врача.");
    }
}

public sealed class UpdateVisitCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdateVisitCommand, Result<VisitDto>>
{
    public async Task<Result<VisitDto>> Handle(UpdateVisitCommand request, CancellationToken cancellationToken)
    {
        var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (visit is null)
            return Result.Failure<VisitDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        visit.UpdateNotes(request.Notes, request.Prescriptions, request.MaterialsUsed);

        if (request.Diagnosis is not null)
            visit.SetDiagnosis(request.Diagnosis);

        if (visit.VisitDate != request.VisitDate || visit.VisitTime != request.VisitTime)
        {
            if (await VisitSlotConflictChecker.HasConflictAsync(
                unitOfWork, request.DoctorName, request.VisitDate, request.VisitTime, excludeVisitId: visit.Id, cancellationToken))
            {
                return Result.Failure<VisitDto>(Error.Conflict("Visit.SlotTaken",
                    $"На {request.VisitDate:dd.MM.yyyy} {request.VisitTime:HH:mm} у врача уже есть визит — выберите другое время."));
            }

            try
            {
                visit.Reschedule(request.VisitDate, request.VisitTime);
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return Result.Failure<VisitDto>(Error.Validation("Visit.CannotReschedule", ex.Message));
            }
        }

        await unitOfWork.AuditLogs.AddAsync(
            Domain.Entities.AuditLog.Create("VISIT_UPDATED", "Visit", visit.Id, null), cancellationToken);

        return Result.Success(mapper.Map<VisitDto>(visit));
    }
}
