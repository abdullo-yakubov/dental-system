using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class Warranty : Entity
{
    public Guid PatientId { get; private set; }
    public Guid VisitId { get; private set; }
    public Guid ProcedureId { get; private set; }
    public short? ToothNumber { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Notes { get; private set; }

    private Warranty()
    {
    }

    public static Warranty Create(Guid patientId, Guid visitId, Guid procedureId, short? toothNumber, DateOnly startDate, int warrantyMonths, string? notes)
    {
        return new Warranty
        {
            PatientId = patientId,
            VisitId = visitId,
            ProcedureId = procedureId,
            ToothNumber = toothNumber,
            StartDate = startDate,
            EndDate = startDate.AddMonths(warrantyMonths),
            Notes = notes
        };
    }
}
