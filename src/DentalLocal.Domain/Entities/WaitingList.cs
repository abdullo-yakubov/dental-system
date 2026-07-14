using DentalLocal.Domain.Common;

namespace DentalLocal.Domain.Entities;

public class WaitingList : Entity
{
    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public DateOnly DesiredDateFrom { get; private set; }
    public DateOnly DesiredDateTo { get; private set; }
    public Guid? ProcedureId { get; private set; }
    public Procedure? Procedure { get; private set; }
    public string? Notes { get; private set; }
    public string Status { get; private set; } = null!; // e.g. "Waiting", "Scheduled", "Cancelled"

    private WaitingList()
    {
    }

    public static WaitingList Create(Guid patientId, DateOnly desiredDateFrom, DateOnly desiredDateTo, Guid? procedureId, string? notes, string status = "Waiting")
    {
        if (desiredDateTo < desiredDateFrom)
            throw new ArgumentException("Конечная желаемая дата не может быть раньше начальной.");

        return new WaitingList
        {
            PatientId = patientId,
            DesiredDateFrom = desiredDateFrom,
            DesiredDateTo = desiredDateTo,
            ProcedureId = procedureId,
            Notes = notes,
            Status = status
        };
    }

    public void UpdateStatus(string status)
    {
        Status = status;
        Touch();
    }
}
