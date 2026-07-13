using DentalLocal.Application.Features.Patients;
using DentalLocal.Domain.Enums;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Patients;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class PatientsEndpoints
{
    public static void MapPatientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Patients.Base).WithTags("Patients");

        group.MapGet("", async (string? search, bool? includeArchived, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SearchPatientsQuery(search, includeArchived ?? false, page, pageSize), ct);
            return result.ToApiResult();
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientByIdQuery(id), ct);
            return result.ToApiResult();
        });

        group.MapPost("", async (CreatePatientRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreatePatientCommand(
                request.LastName, request.FirstName, request.MiddleName, (Gender)(short)request.Gender, request.BirthDate,
                request.Phone, request.PhoneExtra, request.Address, request.Allergies, request.ChronicDiseases,
                request.Comments, request.DoctorNotes);

            var result = await sender.Send(command, ct);
            return result.ToCreatedResult(dto => $"{ApiRoutes.Patients.Base}/{dto.Id}");
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdatePatientRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdatePatientCommand(
                id, request.LastName, request.FirstName, request.MiddleName, (Gender)(short)request.Gender, request.BirthDate,
                request.Phone, request.PhoneExtra, request.Address, request.Allergies, request.ChronicDiseases,
                request.Comments, request.DoctorNotes);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        group.MapPost("/{id:guid}/archive", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ArchivePatientCommand(id), ct);
            return result.ToApiResult();
        });

        group.MapPost("/{id:guid}/unarchive", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UnarchivePatientCommand(id), ct);
            return result.ToApiResult();
        });
    }
}
