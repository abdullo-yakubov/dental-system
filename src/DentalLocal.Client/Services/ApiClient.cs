using System.Net.Http.Json;
using DentalLocal.Client.Http;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Auth;
using DentalLocal.Shared.Contracts.Patients;
using DentalLocal.Shared.Contracts.Procedures;
using DentalLocal.Shared.Contracts.Visits;
using DentalLocal.Shared.Contracts.Teeth;
using DentalLocal.Shared.Contracts.Payments;
using DentalLocal.Shared.Contracts.Analytics;
using DentalLocal.Shared.Contracts.Materials;
using DentalLocal.Shared.Contracts.Backups;
using DentalLocal.Shared.Contracts.Settings;
using DentalLocal.Shared.Contracts.Reminders;
using DentalLocal.Shared.Contracts.TreatmentPlans;
using DentalLocal.Shared.Contracts.Icd;
using DentalLocal.Shared.Contracts.Currencies;
using DentalLocal.Shared.Contracts.Discounts;
using DentalLocal.Shared.Contracts.WaitingList;
using DentalLocal.Shared.Contracts.Prescriptions;
using DentalLocal.Shared.Contracts.Warranties;
using DentalLocal.Shared.Contracts.DoctorSchedule;
using DentalLocal.Shared.Enums;

namespace DentalLocal.Client.Services;

/// <summary>Минимальный срез полей ProblemDetails (RFC 7807), которых достаточно для показа ошибки в UI.</summary>
internal sealed record ProblemDetailsDto(string? Title, int? Status);

public sealed class ApiClient(HttpClient http)
{
    // ── Auth ─────────────────────────────────────────────────────────────────

    public Task<LoginResponse> LoginAsync(string code, CancellationToken ct = default) =>
        SendAsync<LoginResponse>(() => http.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(code), ct));

    public Task ChangeAccessCodeAsync(string oldCode, string newCode, CancellationToken ct = default) =>
        SendVoidAsync(() => http.PostAsJsonAsync(ApiRoutes.Auth.ChangeCode, new ChangeAccessCodeRequest(oldCode, newCode), ct));

    // ── Patients ─────────────────────────────────────────────────────────────

    public Task<PagedResult<PatientListItemDto>> SearchPatientsAsync(
        string? search, bool includeArchived, int page, int pageSize, CancellationToken ct = default)
    {
        var query = $"?search={Uri.EscapeDataString(search ?? string.Empty)}&includeArchived={includeArchived}&page={page}&pageSize={pageSize}";
        return SendAsync<PagedResult<PatientListItemDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}{query}", ct));
    }

    public Task<PatientDto> GetPatientAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<PatientDto>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{id}", ct));

    public Task<IReadOnlyList<PossibleDuplicatePatientDto>> CheckPatientDuplicateAsync(
        string lastName, string firstName, DateOnly birthDate, CancellationToken ct = default)
    {
        var query = $"?lastName={Uri.EscapeDataString(lastName)}&firstName={Uri.EscapeDataString(firstName)}&birthDate={birthDate:yyyy-MM-dd}";
        return SendAsync<IReadOnlyList<PossibleDuplicatePatientDto>>(
            () => http.GetAsync($"{ApiRoutes.Patients.Base}/check-duplicate{query}", ct));
    }

    public Task<IReadOnlyList<PatientTimelineEventDto>> GetPatientTimelineAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PatientTimelineEventDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{patientId}/timeline", ct));

    public Task<IReadOnlyList<PatientListItemDto>> GetInactivePatientsAsync(int monthsThreshold = 6, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PatientListItemDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/inactive?monthsThreshold={monthsThreshold}", ct));

    public Task<PatientDto> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct = default) =>
        SendAsync<PatientDto>(() => http.PostAsJsonAsync(ApiRoutes.Patients.Base, request, ct));

    public Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientRequest request, CancellationToken ct = default) =>
        SendAsync<PatientDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Patients.Base}/{id}", request, ct));

    public Task ArchivePatientAsync(Guid id, CancellationToken ct = default) =>
        SendVoidAsync(() => http.PostAsync($"{ApiRoutes.Patients.Base}/{id}/archive", null, ct));

    public Task UnarchivePatientAsync(Guid id, CancellationToken ct = default) =>
        SendVoidAsync(() => http.PostAsync($"{ApiRoutes.Patients.Base}/{id}/unarchive", null, ct));

    public Task<IReadOnlyList<PatientFileDto>> GetPatientFilesAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PatientFileDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{patientId}/files", ct));

    public async Task<PatientFileDto> UploadPatientFileAsync(Guid patientId, short fileType, string fileName, byte[] fileBytes, string? description, string? beforeAfterTag = null, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeType(fileName));
        content.Add(fileContent, "file", fileName);

        content.Add(new StringContent(fileType.ToString()), "fileType");
        if (description != null)
        {
            content.Add(new StringContent(description), "description");
        }
        if (beforeAfterTag != null)
        {
            content.Add(new StringContent(beforeAfterTag), "beforeAfterTag");
        }

        using var response = await http.PostAsync($"{ApiRoutes.Patients.Base}/{patientId}/files", content, ct);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PatientFileDto>())!;
    }

    public Task DeletePatientFileAsync(Guid fileId, CancellationToken ct = default) =>
        SendVoidAsync(() => http.DeleteAsync($"{ApiRoutes.Patients.Base}/files/{fileId}", ct));

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    // ── Visits ───────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<VisitListItemDto>> GetPatientVisitsAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<VisitListItemDto>>(() => http.GetAsync($"{ApiRoutes.Visits.Base}?patientId={patientId}", ct));

    public Task<IReadOnlyList<VisitListItemDto>> GetUpcomingVisitsAsync(int daysAhead = 7, DateOnly? fromDate = null, CancellationToken ct = default)
    {
        var query = $"?daysAhead={daysAhead}" + (fromDate.HasValue ? $"&fromDate={fromDate.Value:yyyy-MM-dd}" : "");
        return SendAsync<IReadOnlyList<VisitListItemDto>>(() => http.GetAsync($"{ApiRoutes.Visits.Base}/upcoming{query}", ct));
    }

    public Task<PagedResult<VisitListItemDto>> SearchVisitsByNotesAsync(
        string searchText, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = $"?text={Uri.EscapeDataString(searchText)}&page={page}&pageSize={pageSize}";
        return SendAsync<PagedResult<VisitListItemDto>>(() => http.GetAsync($"{ApiRoutes.Visits.Base}/search-notes{query}", ct));
    }

    public Task<VisitDto> GetVisitAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.GetAsync($"{ApiRoutes.Visits.Base}/{id}", ct));

    public Task<VisitDto> CreateVisitAsync(CreateVisitRequest request, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.PostAsJsonAsync(ApiRoutes.Visits.Base, request, ct));

    public Task<VisitDto> UpdateVisitAsync(Guid id, UpdateVisitRequest request, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Visits.Base}/{id}", request, ct));

    public Task<VisitDto> ConfirmVisitAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.PostAsync($"{ApiRoutes.Visits.Base}/{id}/confirm", null, ct));

    public Task<VisitDto> CancelVisitAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.PostAsync($"{ApiRoutes.Visits.Base}/{id}/cancel", null, ct));

    public async Task<byte[]> GetVisitInvoicePdfAsync(Guid visitId, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"{ApiRoutes.Reports.Base}/visits/{visitId}/invoice", ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // ── Excel export ─────────────────────────────────────────────────────────

    public async Task<byte[]> ExportDebtorsExcelAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"{ApiRoutes.Reports.Base}/debtors/export", ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> ExportTopProceduresExcelAsync(int count = 20, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"{ApiRoutes.Reports.Base}/procedures/export?count={count}", ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> ExportWorkloadExcelAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var query = (from.HasValue ? $"?from={from.Value:yyyy-MM-dd}" : "") + (to.HasValue ? $"{(from.HasValue ? "&" : "?")}to={to.Value:yyyy-MM-dd}" : "");
        using var response = await http.GetAsync($"{ApiRoutes.Reports.Base}/workload/export{query}", ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // ── Reminders ────────────────────────────────────────────────────────────

    public Task<ReminderDto> CreateReminderAsync(CreateReminderRequest request, CancellationToken ct = default) =>
        SendAsync<ReminderDto>(() => http.PostAsJsonAsync(ApiRoutes.Reminders.Base, request, ct));

    // ── Treatment Plans ──────────────────────────────────────────────────────

    public Task<IReadOnlyList<TreatmentPlanDto>> GetPatientTreatmentPlansAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<TreatmentPlanDto>>(() => http.GetAsync($"{ApiRoutes.TreatmentPlans.Base}?patientId={patientId}", ct));

    public Task<TreatmentPlanDto> GetTreatmentPlanAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<TreatmentPlanDto>(() => http.GetAsync($"{ApiRoutes.TreatmentPlans.Base}/{id}", ct));

    public Task<TreatmentPlanDto> CreateTreatmentPlanAsync(CreateTreatmentPlanRequest request, CancellationToken ct = default) =>
        SendAsync<TreatmentPlanDto>(() => http.PostAsJsonAsync(ApiRoutes.TreatmentPlans.Base, request, ct));

    public Task<TreatmentPlanDto> UpdateTreatmentPlanAsync(Guid id, UpdateTreatmentPlanRequest request, CancellationToken ct = default) =>
        SendAsync<TreatmentPlanDto>(() => http.PutAsJsonAsync($"{ApiRoutes.TreatmentPlans.Base}/{id}", request, ct));

    public Task<TreatmentPlanDto> AddTreatmentPlanItemAsync(Guid id, AddTreatmentPlanItemRequest request, CancellationToken ct = default) =>
        SendAsync<TreatmentPlanDto>(() => http.PostAsJsonAsync($"{ApiRoutes.TreatmentPlans.Base}/{id}/items", request, ct));

    public Task<TreatmentPlanDto> ActivateTreatmentPlanAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<TreatmentPlanDto>(() => http.PostAsync($"{ApiRoutes.TreatmentPlans.Base}/{id}/activate", null, ct));

    public Task<TreatmentPlanDto> CancelTreatmentPlanAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<TreatmentPlanDto>(() => http.PostAsync($"{ApiRoutes.TreatmentPlans.Base}/{id}/cancel", null, ct));

    public Task<TreatmentPlanDto> CompleteTreatmentPlanItemAsync(Guid id, Guid itemId, CompleteTreatmentPlanItemRequest request, CancellationToken ct = default) =>
        SendAsync<TreatmentPlanDto>(() => http.PostAsJsonAsync($"{ApiRoutes.TreatmentPlans.Base}/{id}/items/{itemId}/complete", request, ct));

    // ── Procedures ───────────────────────────────────────────────────────────

    public Task<IReadOnlyList<ProcedureDto>> GetProceduresAsync(bool includeInactive = false, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ProcedureDto>>(() => http.GetAsync($"{ApiRoutes.Procedures.Base}?includeInactive={includeInactive}", ct));

    public Task<ProcedureDto> CreateProcedureAsync(CreateProcedureRequest request, CancellationToken ct = default) =>
        SendAsync<ProcedureDto>(() => http.PostAsJsonAsync(ApiRoutes.Procedures.Base, request, ct));

    public Task<ProcedureDto> UpdateProcedureAsync(Guid id, UpdateProcedureRequest request, CancellationToken ct = default) =>
        SendAsync<ProcedureDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Procedures.Base}/{id}", request, ct));

    public Task<IReadOnlyList<ProcedureCategoryDto>> GetProcedureCategoriesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ProcedureCategoryDto>>(() => http.GetAsync(ApiRoutes.Procedures.Categories, ct));

    public Task<ProcedureCategoryDto> CreateProcedureCategoryAsync(CreateProcedureCategoryRequest request, CancellationToken ct = default) =>
        SendAsync<ProcedureCategoryDto>(() => http.PostAsJsonAsync(ApiRoutes.Procedures.Categories, request, ct));

    // ── Teeth ────────────────────────────────────────────────────────────────

    public Task<OdontogramDto> GetOdontogramAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<OdontogramDto>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{patientId}/odontogram", ct));

    public Task<IReadOnlyList<ToothStateHistoryDto>> GetToothHistoryAsync(Guid patientId, short toothNumber, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ToothStateHistoryDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{patientId}/teeth/{toothNumber}/history", ct));

    public Task<ToothStateDto> UpdateToothAsync(Guid patientId, short toothNumber, UpdateToothRequest request, CancellationToken ct = default) =>
        SendAsync<ToothStateDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Patients.Base}/{patientId}/teeth/{toothNumber}", request, ct));

    public Task<ToothSurfaceHistoryDto> AddToothSurfaceRecordAsync(Guid patientId, short toothNumber, AddToothSurfaceRecordRequest request, CancellationToken ct = default) =>
        SendAsync<ToothSurfaceHistoryDto>(() => http.PostAsJsonAsync($"{ApiRoutes.Patients.Base}/{patientId}/teeth/{toothNumber}/surfaces", request, ct));

    public Task<IReadOnlyList<ToothTimelineEntryDto>> GetToothSurfaceHistoryAsync(Guid patientId, short toothNumber, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ToothTimelineEntryDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{patientId}/teeth/{toothNumber}/surface-history", ct));

    public Task<IReadOnlyList<IcdCodeDto>> SearchIcd10Async(string? search, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<IcdCodeDto>>(() => http.GetAsync($"{ApiRoutes.Icd.Icd10}?search={Uri.EscapeDataString(search ?? string.Empty)}", ct));

    public Task<IReadOnlyList<IcdCodeDto>> SearchIcd11Async(string? search, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<IcdCodeDto>>(() => http.GetAsync($"{ApiRoutes.Icd.Icd11}?search={Uri.EscapeDataString(search ?? string.Empty)}", ct));

    // ── Payments & Debts ─────────────────────────────────────────────────────

    public Task<PaymentDto> RegisterPaymentAsync(RegisterPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(() => http.PostAsJsonAsync(ApiRoutes.Payments.Base, request, ct));

    public Task<PaymentDto> ReversePaymentAsync(Guid paymentId, string? reason, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(() => http.PostAsJsonAsync($"{ApiRoutes.Payments.Base}/{paymentId}/reverse", reason, ct));

    public Task<IReadOnlyList<PaymentDto>> GetPatientPaymentsAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PaymentDto>>(() => http.GetAsync($"{ApiRoutes.Payments.Base}?patientId={patientId}", ct));

    public Task<IReadOnlyList<DebtDto>> GetPatientDebtsAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DebtDto>>(() => http.GetAsync($"{ApiRoutes.Debts.Base}?patientId={patientId}", ct));

    public Task<PagedResult<DebtListItemDto>> SearchDebtsAsync(
        DebtStatus? status, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var statusQuery = status.HasValue ? $"&status={(int)status.Value}" : "";
        var searchQuery = !string.IsNullOrWhiteSpace(search) ? $"&search={Uri.EscapeDataString(search)}" : "";
        var query = $"?page={page}&pageSize={pageSize}{statusQuery}{searchQuery}";
        return SendAsync<PagedResult<DebtListItemDto>>(() => http.GetAsync($"{ApiRoutes.Debts.Base}/search{query}", ct));
    }

    // ── Dashboard & Analytics ────────────────────────────────────────────────

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default) =>
        SendAsync<DashboardSummaryDto>(() => http.GetAsync(ApiRoutes.Dashboard.Summary, ct));

    public Task<IReadOnlyList<IncomeChartPointDto>> GetIncomeChartAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<IncomeChartPointDto>>(() => http.GetAsync(ApiRoutes.Dashboard.IncomeChart, ct));

    public Task<IReadOnlyList<ProcedureTopDto>> GetTopProceduresAsync(int count = 5, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ProcedureTopDto>>(() => http.GetAsync($"{ApiRoutes.Dashboard.ProceduresTop}?count={count}", ct));

    // ── Materials & Inventory ────────────────────────────────────────────────

    public Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(string? search = null, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<MaterialDto>>(() => http.GetAsync($"{ApiRoutes.Materials.Base}{(search != null ? "?search=" + Uri.EscapeDataString(search) : "")}", ct));

    public Task<IReadOnlyList<MaterialDto>> GetLowStockMaterialsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<MaterialDto>>(() => http.GetAsync($"{ApiRoutes.Materials.Base}/low-stock", ct));

    public Task<MaterialDto> GetMaterialAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<MaterialDto>(() => http.GetAsync($"{ApiRoutes.Materials.Base}/{id}", ct));

    public Task<MaterialDto> CreateMaterialAsync(CreateMaterialRequest request, CancellationToken ct = default) =>
        SendAsync<MaterialDto>(() => http.PostAsJsonAsync(ApiRoutes.Materials.Base, request, ct));

    public Task<MaterialDto> UpdateMaterialAsync(Guid id, UpdateMaterialRequest request, CancellationToken ct = default) =>
        SendAsync<MaterialDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Materials.Base}/{id}", request, ct));

    public Task<MaterialDto> AdjustMaterialStockAsync(Guid id, AdjustStockRequest request, CancellationToken ct = default) =>
        SendAsync<MaterialDto>(() => http.PostAsJsonAsync($"{ApiRoutes.Materials.Base}/{id}/adjust", request, ct));

    public Task<IReadOnlyList<MaterialUsageDto>> GetVisitMaterialUsagesAsync(Guid visitId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<MaterialUsageDto>>(() => http.GetAsync($"{ApiRoutes.Materials.Base}/usages?visitId={visitId}", ct));

    public Task<MaterialUsageDto> RecordMaterialUsageAsync(Guid visitId, RecordMaterialUsageRequest request, CancellationToken ct = default) =>
        SendAsync<MaterialUsageDto>(() => http.PostAsJsonAsync($"{ApiRoutes.Materials.Base}/usages?visitId={visitId}", request, ct));

    // ── Backups ─────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<BackupDto>> GetBackupsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<BackupDto>>(() => http.GetAsync(ApiRoutes.Backups.Base, ct));

    public Task<string> CreateBackupAsync(CreateBackupRequest request, CancellationToken ct = default) =>
        SendAsync<string>(() => http.PostAsJsonAsync(ApiRoutes.Backups.Base, request, ct));

    public Task RestoreBackupAsync(RestoreBackupRequest request, CancellationToken ct = default) =>
        SendVoidAsync(() => http.PostAsJsonAsync($"{ApiRoutes.Backups.Base}/restore", request, ct));

    public Task<bool> ValidateBackupAsync(string fileName, CancellationToken ct = default) =>
        SendAsync<bool>(() => http.GetAsync($"{ApiRoutes.Backups.Base}/validate?fileName={Uri.EscapeDataString(fileName)}", ct));

    // ── Settings & Audit ─────────────────────────────────────────────────────

    public Task<SettingsDto> GetSettingsAsync(CancellationToken ct = default) =>
        SendAsync<SettingsDto>(() => http.GetAsync(ApiRoutes.Settings.Base, ct));

    public Task<SettingsDto> UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken ct = default) =>
        SendAsync<SettingsDto>(() => http.PutAsJsonAsync(ApiRoutes.Settings.Base, request, ct));

    public Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(int page, int pageSize, CancellationToken ct = default) =>
        SendAsync<PagedResult<AuditLogDto>>(() => http.GetAsync($"{ApiRoutes.AuditLogs.Base}?page={page}&pageSize={pageSize}", ct));

    // ── Currencies ───────────────────────────────────────────────────────────

    public Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CurrencyDto>>(() => http.GetAsync(ApiRoutes.Currencies.Base, ct));

    public Task<CurrencyDto> CreateCurrencyAsync(CreateCurrencyRequest request, CancellationToken ct = default) =>
        SendAsync<CurrencyDto>(() => http.PostAsJsonAsync(ApiRoutes.Currencies.Base, request, ct));

    public Task<CurrencyDto> UpdateCurrencyAsync(Guid id, UpdateCurrencyRequest request, CancellationToken ct = default) =>
        SendAsync<CurrencyDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Currencies.Base}/{id}", request, ct));

    // ── Discounts ────────────────────────────────────────────────────────────

    public Task<IReadOnlyList<DiscountHistoryDto>> GetPatientDiscountHistoryAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DiscountHistoryDto>>(() => http.GetAsync($"{ApiRoutes.Discounts.Base}/patients/{patientId}", ct));

    public Task<DiscountHistoryDto> ApplyDiscountAsync(Guid patientId, ApplyDiscountRequest request, CancellationToken ct = default) =>
        SendAsync<DiscountHistoryDto>(() => http.PostAsJsonAsync($"{ApiRoutes.Discounts.Base}/patients/{patientId}", request, ct));

    // ── Waiting list ─────────────────────────────────────────────────────────

    public Task<IReadOnlyList<WaitingListEntryDto>> GetWaitingListAsync(string? status = null, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<WaitingListEntryDto>>(() =>
            http.GetAsync($"{ApiRoutes.WaitingList.Base}{(status is null ? "" : $"?status={Uri.EscapeDataString(status)}")}", ct));

    public Task<WaitingListEntryDto> CreateWaitingListEntryAsync(CreateWaitingListEntryRequest request, CancellationToken ct = default) =>
        SendAsync<WaitingListEntryDto>(() => http.PostAsJsonAsync(ApiRoutes.WaitingList.Base, request, ct));

    public Task<WaitingListEntryDto> UpdateWaitingListStatusAsync(Guid id, string status, CancellationToken ct = default) =>
        SendAsync<WaitingListEntryDto>(() => http.PutAsJsonAsync($"{ApiRoutes.WaitingList.Base}/{id}/status", new UpdateWaitingListStatusRequest(status), ct));

    // ── Prescriptions ────────────────────────────────────────────────────────

    public Task<IReadOnlyList<PrescriptionDto>> GetVisitPrescriptionsAsync(Guid visitId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PrescriptionDto>>(() => http.GetAsync($"{ApiRoutes.Prescriptions.Base}/visits/{visitId}", ct));

    public Task<PrescriptionDto> CreatePrescriptionAsync(Guid visitId, CreatePrescriptionRequest request, CancellationToken ct = default) =>
        SendAsync<PrescriptionDto>(() => http.PostAsJsonAsync($"{ApiRoutes.Prescriptions.Base}/visits/{visitId}", request, ct));

    public Task DeletePrescriptionAsync(Guid id, CancellationToken ct = default) =>
        SendVoidAsync(() => http.DeleteAsync($"{ApiRoutes.Prescriptions.Base}/{id}", ct));

    // ── Warranties ───────────────────────────────────────────────────────────

    public Task<IReadOnlyList<WarrantyDto>> GetPatientWarrantiesAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<WarrantyDto>>(() => http.GetAsync($"{ApiRoutes.Warranties.Base}/patients/{patientId}", ct));

    public Task<IReadOnlyList<WarrantyDto>> GetToothWarrantiesAsync(Guid patientId, short toothNumber, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<WarrantyDto>>(() => http.GetAsync($"{ApiRoutes.Warranties.Base}/patients/{patientId}/teeth/{toothNumber}", ct));

    // ── DoctorSchedule ───────────────────────────────────────────────────────

    public Task<IReadOnlyList<DoctorScheduleDto>> GetDoctorScheduleAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DoctorScheduleDto>>(() => http.GetAsync(ApiRoutes.DoctorSchedule.Base, ct));

    public Task<DoctorScheduleDto> UpsertDoctorScheduleAsync(UpsertDoctorScheduleRequest request, CancellationToken ct = default) =>
        SendAsync<DoctorScheduleDto>(() => http.PutAsJsonAsync(ApiRoutes.DoctorSchedule.Base, request, ct));

    // ── Analytics: workload ──────────────────────────────────────────────────

    public Task<DoctorWorkloadStatsDto> GetDoctorWorkloadStatsAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var query = "";
        if (from.HasValue && to.HasValue)
            query = $"?from={from.Value:yyyy-MM-dd}&to={to.Value:yyyy-MM-dd}";

        return SendAsync<DoctorWorkloadStatsDto>(() => http.GetAsync($"{ApiRoutes.Analytics.Base}/workload{query}", ct));
    }

    private static async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        using var response = await send();
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task SendVoidAsync(Func<Task<HttpResponseMessage>> send)
    {
        using var response = await send();
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>();
        throw new ApiException(problem?.Title ?? "Не удалось выполнить запрос.", (int)response.StatusCode);
    }
}
