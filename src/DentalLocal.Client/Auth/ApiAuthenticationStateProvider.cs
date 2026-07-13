using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace DentalLocal.Client.Auth;

public sealed class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthState _authState;
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public ApiAuthenticationStateProvider(AuthState authState)
    {
        _authState = authState;
        _authState.Changed += () => NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(BuildState());

    private AuthenticationState BuildState()
    {
        if (!_authState.IsAuthenticated)
            return new AuthenticationState(Anonymous);

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "doctor")], authenticationType: "ApiToken");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
