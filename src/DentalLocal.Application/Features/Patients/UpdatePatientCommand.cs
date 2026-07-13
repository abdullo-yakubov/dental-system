using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Domain.Enums;
using DentalLocal.Shared.Contracts.Patients;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string LastName,
    string FirstName,
    string? MiddleName,
    Gender Gender,
    DateOnly BirthDate,
    string Phone,
    string? PhoneExtra,
    string? Address,
    string? Allergies,
    string? ChronicDiseases,
    string? Comments,
    string? DoctorNotes) : ICommand<PatientDto>;

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).WithMessage("Укажите фамилию пациента.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).WithMessage("Укажите имя пациента.");
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Укажите телефон пациента.");
        RuleFor(x => x.BirthDate).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Дата рождения не может быть в будущем.");
    }
}

public sealed class UpdatePatientCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<UpdatePatientCommand, Result<PatientDto>>
{
    public async Task<Result<PatientDto>> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<PatientDto>(Error.NotFound("Patient.NotFound", "Пациент не найден."));

        if (await unitOfWork.Patients.PhoneExistsAsync(request.Phone, request.PatientId, cancellationToken))
            return Result.Failure<PatientDto>(Error.Conflict("Patient.PhoneExists", "Пациент с таким номером телефона уже зарегистрирован."));

        try
        {
            patient.Update(
                request.LastName, request.FirstName, request.MiddleName, request.Gender, request.BirthDate,
                request.Phone, request.PhoneExtra, request.Address, request.Allergies, request.ChronicDiseases,
                request.Comments, request.DoctorNotes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<PatientDto>(Error.Validation("Patient.InvalidData", ex.Message));
        }

        await unitOfWork.AuditLogs.AddAsync(AuditLog.Create("PATIENT_UPDATED", "Patient", patient.Id, null), cancellationToken);

        return mapper.Map<PatientDto>(patient);
    }
}
