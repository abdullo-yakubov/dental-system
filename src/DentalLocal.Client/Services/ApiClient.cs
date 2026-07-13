using System.Net.Http.Json;
using DentalLocal.Client.Http;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Auth;
using DentalLocal.Shared.Contracts.Patients;

namespace DentalLocal.Client.Services;

/// <summary>Минимальный срез полей ProblemDetails (RFC 7807), которых достаточно для показа ошибки в UI.</summary>
internal sealed record ProblemDetailsDto(string? Title, int? Status);

public sealed class ApiClient(HttpClient http)
{
    public Task<LoginResponse> LoginAsync(string code, CancellationToken ct = default) =>
        SendAsync<LoginResponse>(() => http.PostAsJsonAsync(ApiRoutes.Auth.Login, new LoginRequest(code), ct));

    public Task ChangeAccessCodeAsync(string oldCode, string newCode, CancellationToken ct = default) =>
        SendVoidAsync(() => http.PostAsJsonAsync(ApiRoutes.Auth.ChangeCode, new ChangeAccessCodeRequest(oldCode, newCode), ct));

    public Task<PagedResult<PatientListItemDto>> SearchPatientsAsync(
        string? search, bool includeArchived, int page, int pageSize, CancellationToken ct = default)
    {
        var query = $"?search={Uri.EscapeDataString(search ?? string.Empty)}&includeArchived={includeArchived}&page={page}&pageSize={pageSize}";
        return SendAsync<PagedResult<PatientListItemDto>>(() => http.GetAsync($"{ApiRoutes.Patients.Base}{query}", ct));
    }

    public Task<PatientDto> GetPatientAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<PatientDto>(() => http.GetAsync($"{ApiRoutes.Patients.Base}/{id}", ct));

    public Task<PatientDto> CreatePatientAsync(CreatePatientRequest request, CancellationToken ct = default) =>
        SendAsync<PatientDto>(() => http.PostAsJsonAsync(ApiRoutes.Patients.Base, request, ct));

    public Task<PatientDto> UpdatePatientAsync(Guid id, UpdatePatientRequest request, CancellationToken ct = default) =>
        SendAsync<PatientDto>(() => http.PutAsJsonAsync($"{ApiRoutes.Patients.Base}/{id}", request, ct));

    public Task ArchivePatientAsync(Guid id, CancellationToken ct = default) =>
        SendVoidAsync(() => http.PostAsync($"{ApiRoutes.Patients.Base}/{id}/archive", null, ct));

    public Task UnarchivePatientAsync(Guid id, CancellationToken ct = default) =>
        SendVoidAsync(() => http.PostAsync($"{ApiRoutes.Patients.Base}/{id}/unarchive", null, ct));

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
