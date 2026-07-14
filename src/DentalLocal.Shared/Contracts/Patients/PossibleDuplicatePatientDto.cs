namespace DentalLocal.Shared.Contracts.Patients;

public sealed record PossibleDuplicatePatientDto(Guid Id, string FullName, DateOnly BirthDate, string Phone);
