using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.Patients;

public sealed record UpdatePatientRequest(
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
    string? DoctorNotes);
