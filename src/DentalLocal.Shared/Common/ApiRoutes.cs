namespace DentalLocal.Shared.Common;

public static class ApiRoutes
{
    public const string Prefix = "/api/v1";

    public static class Auth
    {
        public const string Login = $"{Prefix}/auth/login";
        public const string ChangeCode = $"{Prefix}/auth/change-code";
        public const string GenerateRecoveryCode = $"{Prefix}/auth/recovery-code";
        public const string ResetWithRecoveryCode = $"{Prefix}/auth/reset-with-recovery-code";
    }

    public static class Dashboard
    {
        public const string Summary = $"{Prefix}/dashboard/summary";
        public const string Upcoming = $"{Prefix}/dashboard/upcoming";
        public const string RecentPatients = $"{Prefix}/dashboard/recent-patients";
        public const string IncomeChart = $"{Prefix}/dashboard/income-chart";
        public const string ProceduresTop = $"{Prefix}/dashboard/procedures-top";
    }

    public static class Patients
    {
        public const string Base = $"{Prefix}/patients";
    }

    public static class Files
    {
        public const string Base = $"{Prefix}/files";
    }

    public static class Visits
    {
        public const string Base = $"{Prefix}/visits";
    }

    public static class Procedures
    {
        public const string Base = $"{Prefix}/procedures";
        public const string Categories = $"{Prefix}/procedure-categories";
    }

    public static class Teeth
    {
        public const string Base = $"{Prefix}/teeth";
    }

    public static class Icd
    {
        public const string Icd10 = $"{Prefix}/icd10";
        public const string Icd11 = $"{Prefix}/icd11";
    }

    public static class Debts
    {
        public const string Base = $"{Prefix}/debts";
    }

    public static class Payments
    {
        public const string Base = $"{Prefix}/payments";
    }

    public static class Appointments
    {
        public const string Base = $"{Prefix}/appointments";
    }

    public static class Analytics
    {
        public const string Base = $"{Prefix}/analytics";
    }

    public static class Materials
    {
        public const string Base = $"{Prefix}/materials";
    }

    public static class Reminders
    {
        public const string Base = $"{Prefix}/reminders";
    }

    public static class TreatmentPlans
    {
        public const string Base = $"{Prefix}/treatment-plans";
    }

    public static class Reports
    {
        public const string Base = $"{Prefix}/reports";
    }

    public static class Backups
    {
        public const string Base = $"{Prefix}/backups";
    }

    public static class Settings
    {
        public const string Base = $"{Prefix}/settings";
    }

    public static class AuditLogs
    {
        public const string Base = $"{Prefix}/audit-logs";
    }

    public static class System
    {
        public const string Health = $"{Prefix}/system/health";

        // Вне /api/v1 намеренно: BearerAuthMiddleware проверяет токен только для путей под Prefix,
        // а health-пробы (Launcher, оркестраторы) не должны знать access-токен, чтобы просто
        // определить, жив ли процесс и готова ли БД — см. промпт 9 аудита зависаний.
        public const string Live = "/health/live";
        public const string Ready = "/health/ready";
    }

    public static class DoctorSchedule
    {
        public const string Base = $"{Prefix}/doctor-schedule";
    }

    public static class Discounts
    {
        public const string Base = $"{Prefix}/discounts";
    }

    public static class WaitingList
    {
        public const string Base = $"{Prefix}/waiting-list";
    }

    public static class Prescriptions
    {
        public const string Base = $"{Prefix}/prescriptions";
    }

    public static class Warranties
    {
        public const string Base = $"{Prefix}/warranties";
    }

    public static class Jobs
    {
        public const string Base = $"{Prefix}/jobs";
    }
}
