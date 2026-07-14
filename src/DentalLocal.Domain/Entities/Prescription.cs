using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class Prescription : Entity, IImmutableRecord
{
    public Guid VisitId { get; private set; }
    public Visit Visit { get; private set; } = null!;
    public string MedicationName { get; private set; } = null!;
    public string Dosage { get; private set; } = null!;
    public string? Instructions { get; private set; }

    private Prescription()
    {
    }

    public static Prescription Create(Guid visitId, string medicationName, string dosage, string? instructions)
    {
        if (string.IsNullOrWhiteSpace(medicationName))
            throw new ArgumentException("Название лекарства обязательно.", nameof(medicationName));
        if (string.IsNullOrWhiteSpace(dosage))
            throw new ArgumentException("Дозировка обязательна.", nameof(dosage));

        return new Prescription
        {
            VisitId = visitId,
            MedicationName = medicationName.Trim(),
            Dosage = dosage.Trim(),
            Instructions = instructions
        };
    }
}
