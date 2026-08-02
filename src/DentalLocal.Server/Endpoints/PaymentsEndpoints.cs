using DentalLocal.Application.Common;
using DentalLocal.Application.Features.Payments;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Payments;
using DentalLocal.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DentalLocal.Server.Endpoints;

public static class PaymentsEndpoints
{
    public static void MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var paymentsGroup = app.MapGroup(ApiRoutes.Payments.Base).WithTags("Payments");
        var debtsGroup = app.MapGroup(ApiRoutes.Debts.Base).WithTags("Debts");

        // ── Payments Endpoints ──────────────────────────────────────────────────

        // POST /api/v1/payments — идемпотентно по заголовку Idempotency-Key (см. промпт 17 аудита):
        // двойной клик по «Внести оплату» с одним и тем же ключом создаёт ровно один Payment,
        // повторный запрос получает тот же результат из кэша, а не создаёт новый.
        paymentsGroup.MapPost("", async (
            RegisterPaymentRequest request, HttpRequest httpRequest,
            ISender sender, IIdempotencyStore idempotency, CancellationToken ct) =>
        {
            var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
            var cacheKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : $"payments:register:{idempotencyKey}";

            if (cacheKey is not null && idempotency.TryGet(cacheKey, out var cachedJson))
            {
                return Results.Content(cachedJson, "application/json");
            }

            var command = new RegisterPaymentCommand(
                request.VisitId,
                request.PatientId,
                request.Amount,
                request.Method,
                request.Kind,
                request.Note);

            var result = await sender.Send(command, ct);

            if (cacheKey is not null && result.IsSuccess)
            {
                idempotency.Set(cacheKey, System.Text.Json.JsonSerializer.Serialize(result.Value), TimeSpan.FromHours(24));
            }

            return result.ToApiResult();
        });

        // POST /api/v1/payments/{id}/reverse
        paymentsGroup.MapPost("/{id:guid}/reverse", async (Guid id, [FromBody] string? reason, ISender sender, CancellationToken ct) =>
        {
            var command = new ReversePaymentCommand(id, reason);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        // GET /api/v1/payments?patientId=...
        paymentsGroup.MapGet("", async (Guid patientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientPaymentsQuery(patientId), ct);
            return result.ToApiResult();
        });

        // ── Debts Endpoints ─────────────────────────────────────────────────────

        // GET /api/v1/debts?patientId=...
        debtsGroup.MapGet("", async (Guid patientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientDebtsQuery(patientId), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/debts/search
        debtsGroup.MapGet("/search", async (
            [FromQuery] DebtStatus? status,
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new SearchDebtsQuery(status, search, page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, ct);
            return result.ToApiResult();
        });
    }
}
