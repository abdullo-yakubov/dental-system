using DentalLocal.Application.Features.Currencies;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Currencies;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class CurrenciesEndpoints
{
    public static void MapCurrenciesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Currencies.Base).WithTags("Currencies");

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCurrenciesQuery(), ct);
            return result.ToApiResult();
        });

        group.MapPost("", async (CreateCurrencyRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateCurrencyCommand(request.Code, request.Name, request.Symbol, request.ExchangeRateToBase, request.IsBase);
            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.Currencies.Base}/{dto.Id}");
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCurrencyRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateCurrencyCommand(id, request.Name, request.Symbol, request.ExchangeRateToBase, request.IsBase);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });
    }
}
