using DentalLocal.Application.Features.Auth;
using DentalLocal.Server.Common;
using Microsoft.AspNetCore.RateLimiting;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Auth;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Auth");

        // Отдельная жёсткая политика rate limiting (см. Program.cs, промпт 17 аудита) — брутфорс PIN
        // подбором через curl не должен быть дешевле, чем через форму входа с клиентской блокировкой.
        group.MapPost(ApiRoutes.Auth.Login, async (LoginRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(request.Code), ct);
            return result.ToApiResult();
        }).RequireRateLimiting(RateLimiterPolicies.Login);

        group.MapPost(ApiRoutes.Auth.ChangeCode, async (ChangeAccessCodeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ChangeAccessCodeCommand(request.OldCode, request.NewCode), ct);
            return result.ToApiResult();
        });

        group.MapPost(ApiRoutes.Auth.GenerateRecoveryCode, async (ChangeAccessCodeGenerateRecoveryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerateRecoveryCodeCommand(request.CurrentPin), ct);
            return result.IsSuccess
                ? Results.Ok(new GenerateRecoveryCodeResponse(result.Value))
                : result.ToApiResult();
        });

        // Без Bearer-токена (см. BearerAuthMiddleware.AnonymousPaths) — по определению вызывается,
        // когда пользователь забыл PIN и войти не может.
        group.MapPost(ApiRoutes.Auth.ResetWithRecoveryCode, async (ResetAccessCodeWithRecoveryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResetAccessCodeWithRecoveryCommand(request.RecoveryCode, request.NewCode), ct);
            return result.ToApiResult();
        });
    }
}

public sealed record ChangeAccessCodeGenerateRecoveryRequest(string CurrentPin);
