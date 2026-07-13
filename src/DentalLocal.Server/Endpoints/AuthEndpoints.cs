using DentalLocal.Application.Features.Auth;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Auth;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Auth");

        group.MapPost(ApiRoutes.Auth.Login, async (LoginRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(request.Code), ct);
            return result.ToApiResult();
        });

        group.MapPost(ApiRoutes.Auth.ChangeCode, async (ChangeAccessCodeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ChangeAccessCodeCommand(request.OldCode, request.NewCode), ct);
            return result.ToApiResult();
        });
    }
}
