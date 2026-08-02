using DentalLocal.Application.Common;
using DentalLocal.Application.Features.Patients;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Patients;
using MediatR;

namespace DentalLocal.Server.Endpoints;

/// <summary>
/// Отдаёт файлы пациентов НЕ через анонимный PhysicalFileProvider (см. промпт 11 аудита зависаний/
/// безопасности — раньше любой процесс на машине мог скачать снимок пациента без токена), а через
/// короткоживущую подписанную ссылку: /link выдаёт её авторизованному клиенту на 5 минут, сам файл
/// отдаётся по подписи в query — это единственный путь под /api/v1, не требующий заголовка Authorization
/// (нужно для &lt;img src&gt;, который такие заголовки слать не может).
/// </summary>
public static class FilesEndpoints
{
    public static void MapFilesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Files.Base).WithTags("Files");

        // GET /api/v1/files/{fileId}/link — обычная Bearer-авторизация.
        group.MapGet("/{fileId:guid}/link", async (Guid fileId, ISender sender, ITokenService tokenService, CancellationToken ct) =>
        {
            var fileResult = await sender.Send(new GetPatientFileByIdQuery(fileId), ct);
            if (fileResult.IsFailure)
            {
                IResult failure = fileResult.ToApiResult();
                return failure;
            }

            var (signature, expiresAt) = await tokenService.IssueFileLinkAsync(fileId, ct);
            var url = $"{ApiRoutes.Files.Base}/{fileId}?exp={expiresAt.ToUnixTimeSeconds()}&sig={Uri.EscapeDataString(signature)}";
            return Results.Ok(new FileLinkDto(url, expiresAt));
        });

        // GET /api/v1/files/{fileId}?exp=&sig= — БЕЗ Bearer-токена (см. BearerAuthMiddleware), проверяет
        // подпись сама. Физический путь реконструируется из patientId+fileId+ext по известной схеме
        // хранения, а не берётся из StoredPath напрямую — на случай, если это поле когда-нибудь станет
        // производным от пользовательского ввода.
        group.MapGet("/{fileId:guid}", async (
            Guid fileId, long exp, string sig,
            ISender sender, ITokenService tokenService, IWebHostEnvironment env, CancellationToken ct) =>
        {
            IResult result;

            if (!await tokenService.ValidateFileLinkAsync(fileId, exp, sig, ct))
            {
                result = Results.Unauthorized();
            }
            else
            {
                var fileResult = await sender.Send(new GetPatientFileByIdQuery(fileId), ct);
                if (fileResult.IsFailure)
                {
                    result = fileResult.ToApiResult();
                }
                else
                {
                    var file = fileResult.Value;
                    var ext = Path.GetExtension(file.FileName);
                    var physicalPath = Path.Combine(env.ContentRootPath, "Data", "Files", file.PatientId.ToString(), $"{file.Id}{ext}");

                    result = File.Exists(physicalPath)
                        ? Results.File(physicalPath, file.ContentType, file.FileName)
                        : Results.NotFound();
                }
            }

            return result;
        });
    }
}
