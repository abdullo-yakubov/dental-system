using System.Net.Http.Headers;
using DentalLocal.Client.Auth;

namespace DentalLocal.Client.Http;

public sealed class BearerTokenHandler(AuthState authState) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (authState.Token is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
