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

    public Task<IReadOnlyList<PatientTimelineEventDto>> GetPatientTimelineAsync(Guid patientId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PatientTimelineEventDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{patientId}/timeline", ct));

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

    public async Task<PatientFileDto> UploadPatientFileAsync(Guid patientId, short fileType, string fileName, byte[] fileBytes, string? description, CancellationToken ct = default)
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

    public Task<VisitDto> GetVisitAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.GetAsync($"{ApiRoutes.Visits.Base}/{id}", ct));

    public Task<VisitDto> CreateVisitAsync(CreateVisitRequest request, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.PostAsJsonAsync(ApiRoutes.Visits.Base, request, ct));

    public Task<VisitDto> UpdateVisitAsync(Guid id, UpdateVisitRequest request, CancellationToken ct = default) =>
        SendAsync<VisitDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Visits.Base}/{id}", request, ct));

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
