using DentalLocal.Application.Features.DoctorSchedule;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.DoctorSchedule;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class DoctorScheduleEndpoints
{
    public static void MapDoctorScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.DoctorSchedule.Base).WithTags("DoctorSchedule");

        group.MapGet("", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDoctorScheduleQuery(), ct);
            return result.ToApiResult();
        });

        group.MapPut("", async (UpsertDoctorScheduleRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new UpsertDoctorScheduleCommand(
                request.DoctorName, request.DayOfWeek, request.StartTime, request.EndTime, request.IsActive);
            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });
    }
}
