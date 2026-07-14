using DentalLocal.Application.Features.Reminders;
using DentalLocal.Server.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Reminders;
using MediatR;

namespace DentalLocal.Server.Endpoints;

public static class RemindersEndpoints
{
    public static void MapRemindersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Reminders.Base).WithTags("Reminders");

        // POST /api/v1/reminders
        group.MapPost("", async (CreateReminderRequest request, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateReminderCommand(
                request.PatientId, request.VisitId, request.Title, request.ReminderDate, request.Type, request.Notes);

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        });
    }
}
