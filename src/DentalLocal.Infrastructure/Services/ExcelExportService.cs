using ClosedXML.Excel;
using DentalLocal.Application.Common;
using DentalLocal.Shared.Contracts.Analytics;
using DentalLocal.Shared.Contracts.Payments;

namespace DentalLocal.Infrastructure.Services;

public sealed class ExcelExportService : IExcelExportService
{
    public byte[] ExportDebtors(IReadOnlyList<DebtListItemDto> debtors)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Должники");

        string[] headers = ["Пациент", "Телефон", "Дата рождения", "Сумма долга", "Дата возникновения", "Статус", "Дата визита"];
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var d in debtors)
        {
            sheet.Cell(row, 1).Value = d.PatientFullName;
            sheet.Cell(row, 2).Value = d.PatientPhone;
            sheet.Cell(row, 3).Value = d.PatientBirthDate.ToString("dd.MM.yyyy");
            sheet.Cell(row, 4).Value = d.RemainingAmount;
            sheet.Cell(row, 5).Value = d.AroseAt.ToString("dd.MM.yyyy");
            sheet.Cell(row, 6).Value = d.Status.ToString();
            sheet.Cell(row, 7).Value = d.VisitDate.ToString("dd.MM.yyyy");
            row++;
        }

        sheet.Columns().AdjustToContents();
        return ToBytes(workbook);
    }

    public byte[] ExportTopProcedures(IReadOnlyList<ProcedureTopDto> procedures)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Топ процедур");

        string[] headers = ["Процедура", "Количество", "Сумма"];
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var p in procedures)
        {
            sheet.Cell(row, 1).Value = p.Name;
            sheet.Cell(row, 2).Value = p.Count;
            sheet.Cell(row, 3).Value = p.TotalAmount;
            row++;
        }

        sheet.Columns().AdjustToContents();
        return ToBytes(workbook);
    }

    public byte[] ExportWorkloadStats(DoctorWorkloadStatsDto stats)
    {
        using var workbook = new XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Сводка");
        summarySheet.Cell(1, 1).Value = "Всего визитов";
        summarySheet.Cell(1, 2).Value = stats.TotalVisits;
        summarySheet.Cell(2, 1).Value = "% отменённых/неявок";
        summarySheet.Cell(2, 2).Value = stats.CancelledOrNoShowPercent;
        summarySheet.Cell(3, 1).Value = "% простоя";
        summarySheet.Cell(3, 2).Value = stats.IdlePercent;
        summarySheet.Columns().AdjustToContents();

        var heatmapSheet = workbook.Worksheets.Add("Загрузка по часам");
        heatmapSheet.Cell(1, 1).Value = "День недели";
        heatmapSheet.Cell(1, 2).Value = "Час";
        heatmapSheet.Cell(1, 3).Value = "Визитов";
        var hRow = 2;
        foreach (var cell in stats.Heatmap)
        {
            heatmapSheet.Cell(hRow, 1).Value = cell.DayOfWeek.ToString();
            heatmapSheet.Cell(hRow, 2).Value = cell.Hour;
            heatmapSheet.Cell(hRow, 3).Value = cell.VisitCount;
            hRow++;
        }
        heatmapSheet.Columns().AdjustToContents();

        var durationSheet = workbook.Worksheets.Add("Длительность по процедурам");
        durationSheet.Cell(1, 1).Value = "Процедура";
        durationSheet.Cell(1, 2).Value = "Средняя длительность, мин";
        var dRow = 2;
        foreach (var d in stats.AverageDurationByProcedure)
        {
            durationSheet.Cell(dRow, 1).Value = d.ProcedureName;
            durationSheet.Cell(dRow, 2).Value = d.AverageDurationMinutes;
            dRow++;
        }
        durationSheet.Columns().AdjustToContents();

        return ToBytes(workbook);
    }

    private static byte[] ToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
