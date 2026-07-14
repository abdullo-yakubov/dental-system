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

        // POST /api/v1/payments
        paymentsGroup.MapPost("", async (RegisterPaymentRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new RegisterPaymentCommand(
                request.VisitId,
                request.PatientId,
                request.Amount,
                request.Method,
                request.Kind,
                request.Note);

            var result = await sender.Send(command, ct);
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
