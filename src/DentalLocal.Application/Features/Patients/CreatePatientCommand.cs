using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Domain.Enums;
using DentalLocal.Shared.Contracts.Patients;
using FluentValidation;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

public sealed record CreatePatientCommand(
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

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).WithMessage("Укажите фамилию пациента.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).WithMessage("Укажите имя пациента.");
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Укажите телефон пациента.");
        RuleFor(x => x.BirthDate).LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Дата рождения не может быть в будущем.");
    }
}

public sealed class CreatePatientCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CreatePatientCommand, Result<PatientDto>>
{
    public async Task<Result<PatientDto>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        Patient patient;
        try
        {
            patient = Patient.Create(
                request.LastName, request.FirstName, request.MiddleName, request.Gender, request.BirthDate,
                request.Phone, request.PhoneExtra, request.Address, request.Allergies, request.ChronicDiseases,
                request.Comments, request.DoctorNotes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<PatientDto>(Error.Validation("Patient.InvalidData", ex.Message));
        }

        if (await unitOfWork.Patients.PhoneExistsAsync(patient.Phone, cancellationToken: cancellationToken))
            return Result.Failure<PatientDto>(Error.Conflict("Patient.PhoneExists", "Пациент с таким номером телефона уже зарегистрирован."));

        await unitOfWork.Patients.AddAsync(patient, cancellationToken);

        // Сохраняем сразу, чтобы получить сгенерированный СУБД card_number до маппинга в DTO
        // (TransactionBehavior вызовет SaveChangesAsync повторно перед коммитом — это безопасный no-op).
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.AuditLogs.AddAsync(Domain.Entities.AuditLog.Create("PATIENT_CREATED", "Patient", patient.Id, null), cancellationToken);

        return mapper.Map<PatientDto>(patient);
    }
}
