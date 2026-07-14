using System;

namespace DentalLocal.Persistence.Configurations;

public class PatientDebtView
{
    public Guid PatientId { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public decimal TotalDebt { get; set; }
    public DateTimeOffset? LastDebtDate { get; set; }
    public Guid? LastVisitId { get; set; }
}
