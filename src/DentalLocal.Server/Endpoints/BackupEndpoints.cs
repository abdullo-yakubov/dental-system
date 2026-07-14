using DentalLocal.Application.Features.Backups;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Backups;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class BackupEndpoints
{
    public static void MapBackupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Backups.Base).WithTags("Backups");

        // GET /api/v1/backups
        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetBackupsListQuery(), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/backups
        group.MapPost("", async (CreateBackupRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateManualBackupCommand(request.Pin, request.Note), ct);
            return result.ToApiResult();
        });

        // POST /api/v1/backups/restore
        group.MapPost("/restore", async (RestoreBackupRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RestoreBackupCommand(request.Pin, request.BackupFileName), ct);
            return result.ToApiResult();
        });

        // GET /api/v1/backups/validate
        group.MapGet("/validate", async (string fileName, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ValidateBackupIntegrityCommand(fileName), ct);
            return result.ToApiResult();
        });
    }
}
