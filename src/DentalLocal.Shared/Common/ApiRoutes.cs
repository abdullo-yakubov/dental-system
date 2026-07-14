namespace DentalLocal.Shared.Common;

public static class ApiRoutes
{
    public const string Prefix = "/api/v1";

    public static class Auth
    {
        public const string Login = $"{Prefix}/auth/login";
        public const string ChangeCode = $"{Prefix}/auth/change-code";
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
    }
}
