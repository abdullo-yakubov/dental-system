using DentalLocal.Shared.Contracts.Analytics;
using DentalLocal.Shared.Contracts.Payments;

namespace DentalLocal.Application.Common;

/// <summary>Экспорт табличных отчётов в Excel (разделы 7.6/7.7/17.8 ТЗ).</summary>
public interface IExcelExportService
{
    byte[] ExportDebtors(IReadOnlyList<DebtListItemDto> debtors);

    byte[] ExportTopProcedures(IReadOnlyList<ProcedureTopDto> procedures);

    byte[] ExportWorkloadStats(DoctorWorkloadStatsDto stats);
}
