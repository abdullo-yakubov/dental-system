using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DentalLocal.Server.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports").WithTags("Reports");

        // GET /api/v1/reports/patients/{id}/agreement
        group.MapGet("/patients/{id:guid}/agreement", async (Guid id, IUnitOfWork unitOfWork, IPdfService pdfService, CancellationToken ct) =>
        {
            var patient = await unitOfWork.Patients.GetByIdAsync(id, ct);
            if (patient == null)
            {
                return Results.NotFound(new { error = "Пациент не найден." });
            }

            var pdfBytes = pdfService.GenerateAgreementPdf(patient);
            return Results.File(pdfBytes, "application/pdf", $"Договор_{patient.LastName}.pdf");
        });

        // GET /api/v1/reports/patients/{id}/consent
        group.MapGet("/patients/{id:guid}/consent", async (Guid id, IUnitOfWork unitOfWork, IPdfService pdfService, CancellationToken ct) =>
        {
            var patient = await unitOfWork.Patients.GetByIdAsync(id, ct);
            if (patient == null)
            {
                return Results.NotFound(new { error = "Пациент не найден." });
            }

            var pdfBytes = pdfService.GenerateConsentPdf(patient);
            return Results.File(pdfBytes, "application/pdf", $"Согласие_{patient.LastName}.pdf");
        });

        // GET /api/v1/reports/visits/{id}/prescription
        group.MapGet("/visits/{id:guid}/prescription", async (Guid id, IUnitOfWork unitOfWork, IPdfService pdfService, CancellationToken ct) =>
        {
            var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(id, ct);
            if (visit == null)
            {
                return Results.NotFound(new { error = "Визит не найден." });
            }

            var pdfBytes = pdfService.GeneratePrescriptionPdf(visit);
            return Results.File(pdfBytes, "application/pdf", $"Рецепт_{visit.Patient?.LastName ?? ""}.pdf");
        });

        // GET /api/v1/reports/visits/{id}/invoice
        group.MapGet("/visits/{id:guid}/invoice", async (Guid id, IUnitOfWork unitOfWork, IPdfService pdfService, CancellationToken ct) =>
        {
            var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(id, ct);
            if (visit == null)
            {
                return Results.NotFound(new { error = "Визит не найден." });
            }

            var pdfBytes = pdfService.GenerateInvoicePdf(visit);
            return Results.File(pdfBytes, "application/pdf", $"Чек_{visit.Patient?.LastName ?? ""}.pdf");
        });
    }
}
