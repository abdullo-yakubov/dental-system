using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
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
    IReadOnlyList<CreateVisitItemCommand> Items) : ICommand<VisitDto>;

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
        RuleFor(x => x.VisitDate).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("Дата визита не может быть позже чем завтра.");
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
        try
        {
            visit = Visit.Create(
                request.PatientId,
                request.VisitDate,
                request.VisitTime,
                request.DoctorName,
                request.Notes,
                request.Prescriptions,
                request.MaterialsUsed);

            foreach (var item in request.Items)
            {
                var procedure = await unitOfWork.Procedures.GetByIdAsync(item.ProcedureId, cancellationToken);
                if (procedure is null)
                    return Result.Failure<VisitDto>(Error.NotFound("Procedure.NotFound", $"Процедура {item.ProcedureId} не найдена."));

                visit.AddItem(item.ProcedureId, item.ToothNumber, item.Price, item.Quantity, item.Comment);
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

        if (visit.RemainingAmount > 0)
        {
            var debt = Debt.Create(visit.Id, visit.PatientId, visit.RemainingAmount);
            await unitOfWork.Debts.AddAsync(debt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("VISIT_CREATED", "Visit", visit.Id, null), cancellationToken);

        var createdVisit = await unitOfWork.Visits.GetByIdWithItemsAsync(visit.Id, cancellationToken);
        return Result.Success(mapper.Map<VisitDto>(createdVisit));
    }
}
