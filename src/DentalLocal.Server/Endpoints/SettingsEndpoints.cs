using DentalLocal.Application.Features.Settings;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Settings;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Settings.Base).WithTags("Settings");

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSettingsQuery(), ct);
            return result.ToApiResult();
        });

        group.MapPut("", async (UpdateSettingsRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateSettingsCommand(
                request.DoctorName, request.ClinicName, request.Currency,
                request.Theme, request.Language, request.BackupTime, request.BackupRetentionDays,
                request.AutoLockMinutes);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });

        var auditGroup = app.MapGroup(ApiRoutes.AuditLogs.Base).WithTags("Settings");

        auditGroup.MapGet("", async (int? page, int? pageSize, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAuditLogsQuery(page, pageSize), ct);
            return result.ToApiResult();
        });
    }
}
