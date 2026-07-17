using DentalLocal.Application.Features.Visits;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Visits;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class VisitsEndpoints
{
    public static void MapVisitsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Visits.Base).WithTags("Visits");

        // GET /api/v1/visits/upcoming?daysAhead=7&fromDate=...
        group.MapGet("/upcoming", async (int? daysAhead, DateOnly? fromDate, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUpcomingVisitsQuery(daysAhead ?? 7, fromDate), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/visits/search-notes?text=...&page=1&pageSize=20
        group.MapGet("/search-notes", async (string text, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SearchVisitsByNotesQuery(text, page, pageSize), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/visits/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetVisitByIdQuery(id), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/visits?patientId=...
        group.MapGet("", async (Guid patientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientVisitsQuery(patientId), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/visits
        group.MapPost("", async (CreateVisitRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateVisitCommand(
                request.PatientId,
                request.VisitDate,
                request.VisitTime,
                request.DoctorName,
                request.Notes,
                request.Prescriptions,
                request.MaterialsUsed,
                request.Items.Select(i => new CreateVisitItemCommand(
                    i.ProcedureId, i.ToothNumber, i.Price, i.Quantity, i.Comment)).ToList(),
                request.Diagnosis);

            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.Visits.Base}/{dto.Id}");
        });

        // PUT /api/v1/visits/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateVisitRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateVisitCommand(
                id,
                request.VisitDate,
                request.VisitTime,
                request.DoctorName,
                request.Notes,
                request.Prescriptions,
                request.MaterialsUsed,
                request.Diagnosis);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        // POST /api/v1/visits/{id}/confirm
        group.MapPost("/{id:guid}/confirm", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ConfirmVisitCommand(id), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/visits/{id}/cancel
        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CancelVisitCommand(id), ct);
            return result.ToApiResult();
        });
    }
}
