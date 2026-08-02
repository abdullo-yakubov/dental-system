using DentalLocal.Application.Common;
using DentalLocal.Shared.Common;
using DentalLocal.Shared.Contracts.Jobs;

namespace DentalLocal.Server.Endpoints;

public static class JobsEndpoints
{
    public static void MapJobsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Jobs.Base).WithTags("Jobs");

        // GET /api/v1/jobs/{id}
        group.MapGet("/{id:guid}", (Guid id, IJobQueue queue) =>
        {
            var job = queue.GetJob(id);
            return job is null ? Results.NotFound() : Results.Ok(ToDto(job));
        });

        // GET /api/v1/jobs?status=Running
        group.MapGet("", (string? status, IJobQueue queue) =>
        {
            JobStatus? parsedStatus = !string.IsNullOrWhiteSpace(status) && Enum.TryParse<JobStatus>(status, true, out var s)
                ? s
                : null;

            var jobs = queue.GetJobs(parsedStatus).Select(ToDto).ToList();
            return Results.Ok(jobs);
        });
    }

    private static JobDto ToDto(JobRecord job) =>
        new(job.Id, job.Type, job.Status.ToString(), job.Progress, job.Error, job.CreatedAt, job.FinishedAt);
}
