using System.Net;
using System.Net.Http.Headers;
using DentalLocal.Client.Auth;

namespace DentalLocal.Client.Http;

public sealed class BearerTokenHandler(AuthState authState) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (authState.Token is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        // Токен сессии не обновляется (см. AuthState — 12ч, без refresh-токена), поэтому 401 здесь
        // означает "сессия истекла/невалидна" безусловно — никаких повторных попыток или циклов
        // обновления токена, сразу разлогиниваем (см. промпт 43 аудита). TryHandleUnauthorizedOnce
        // гарантирует, что при шквале параллельных 401 разлогин выполнится ровно один раз.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            authState.TryHandleUnauthorizedOnce();
        }

        return response;
    }
}
