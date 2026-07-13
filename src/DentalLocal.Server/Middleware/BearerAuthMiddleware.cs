using DentalLocal.Application.Common;
using DentalLocal.Shared.Common;
using Microsoft.AspNetCore.Http;

namespace DentalLocal.Server.Middleware;

/// <summary>
/// Проверяет Bearer-токен для всех эндпоинтов под /api/v1, кроме входа. Backend слушает только 127.0.0.1
/// (см. п.17 ТЗ) — этой проверки достаточно, полноценный ASP.NET Core Identity/Authorization не нужен
/// для однопользовательской локальной системы.
/// </summary>
public sealed class BearerAuthMiddleware(RequestDelegate next)
{
    private static readonly string[] AnonymousPaths = [ApiRoutes.Auth.Login, ApiRoutes.System.Health];

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        var requiresAuth = path.StartsWith(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase)
            && !AnonymousPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));

        if (!requiresAuth)
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var token = header["Bearer ".Length..].Trim();
        if (!await tokenService.ValidateAsync(token, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
