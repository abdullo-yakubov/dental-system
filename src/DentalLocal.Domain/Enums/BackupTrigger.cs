namespace DentalLocal.Domain.Enums;

public enum BackupTrigger : short
{
    DailyAuto = 1,
    BeforeUpdate = 2,
    Manual = 3,
    BeforeRestore = 4,
}
