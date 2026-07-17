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

        // GET /api/v1/patients/check-duplicate?lastName=...&firstName=...&birthDate=...
        group.MapGet("/check-duplicate", async (string lastName, string firstName, DateOnly birthDate, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CheckPatientDuplicateQuery(lastName, firstName, birthDate), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/patients/inactive?monthsThreshold=6
        group.MapGet("/inactive", async (int? monthsThreshold, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetInactivePatientsQuery(monthsThreshold ?? 6), ct);
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

        // GET /api/v1/patients/{id}/timeline
        group.MapGet("/{id:guid}/timeline", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientTimelineQuery(id), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/patients/{id}/files
        group.MapGet("/{id:guid}/files", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPatientFilesQuery(id), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/patients/{id}/files
        group.MapPost("/{id:guid}/files", async (Guid id, HttpRequest request, ISender sender) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Invalid content type.");

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest("No file uploaded.");

            short fileType = 0;
            if (form.TryGetValue("fileType", out var ftVal) && short.TryParse(ftVal, out var ft))
            {
                fileType = ft;
            }

            string? description = form.TryGetValue("description", out var descVal) ? descVal.ToString() : null;
            string? beforeAfterTag = form.TryGetValue("beforeAfterTag", out var tagVal) && !string.IsNullOrWhiteSpace(tagVal)
                ? tagVal.ToString()
                : null;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var command = new UploadPatientFileCommand(
                id,
                fileType,
                file.FileName,
                file.ContentType,
                file.Length,
                fileBytes,
                description,
                beforeAfterTag);

            var result = await sender.Send(command);
            return result.ToApiResult();
        }).DisableAntiforgery();

        // DELETE /api/v1/patients/files/{id}
        app.MapDelete($"{ApiRoutes.Patients.Base}/files/{{id:guid}}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeletePatientFileCommand(id));
            return result.ToApiResult();
        }).WithTags("Patients");
    }
}
