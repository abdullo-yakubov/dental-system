using DentalLocal.Application.Common;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Patients;
using MediatR;

namespace DentalLocal.Application.Features.Patients;

public sealed record GetPatientTimelineQuery(Guid PatientId) : IQuery<IReadOnlyList<PatientTimelineEventDto>>;

public sealed class GetPatientTimelineQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPatientTimelineQuery, Result<IReadOnlyList<PatientTimelineEventDto>>>
{
    public async Task<Result<IReadOnlyList<PatientTimelineEventDto>>> Handle(
        GetPatientTimelineQuery request, CancellationToken cancellationToken)
    {
        var patient = await unitOfWork.Patients.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure<IReadOnlyList<PatientTimelineEventDto>>(
                Error.NotFound("Patient.NotFound", "Пациент не найден."));

        var events = new List<PatientTimelineEventDto>();

        // 1. Visits
        var visits = await unitOfWork.Visits.GetByPatientIdAsync(request.PatientId, cancellationToken);
        foreach (var visit in visits)
        {
            var desc = $"Врач: {visit.DoctorName}";
            if (!string.IsNullOrWhiteSpace(visit.Notes))
                desc += $", Заметки: {visit.Notes}";
            desc += $", Стоимость: {visit.TotalCost:N0} сомони";

            var visitTimestamp = new DateTimeOffset(
                visit.VisitDate.ToDateTime(visit.VisitTime, DateTimeKind.Unspecified),
                TimeSpan.Zero);

            events.Add(new PatientTimelineEventDto(
                visit.Id,
                "Визит завершен",
                desc,
                visitTimestamp,
                "Icons.Material.Filled.Event",
                "Primary"));
        }

        // 2. Payments
        var payments = await unitOfWork.Payments.GetByPatientIdAsync(request.PatientId, cancellationToken);
        foreach (var payment in payments)
        {
            var isReversal = payment.ReversalOfPaymentId is not null;
            var typeLabel = isReversal ? "Возврат средств" : "Оплата услуг";
            var desc = $"Сумма: {payment.Amount:N0} сомони ({payment.Method})";
            if (!string.IsNullOrWhiteSpace(payment.Note))
                desc += $", Комментарий: {payment.Note}";

            var color = isReversal ? "Warning" : "Success";
            var icon = isReversal ? "Icons.Material.Filled.Undo" : "Icons.Material.Filled.Payments";

            events.Add(new PatientTimelineEventDto(
                payment.Id,
                typeLabel,
                desc,
                payment.PaidAt,
                icon,
                color));
        }

        // 3. Tooth status changes
        var toothHistory = await unitOfWork.ToothStateHistory.GetByPatientIdAsync(request.PatientId, cancellationToken);
        foreach (var history in toothHistory)
        {
            var desc = $"Состояние зуба изменено на «{history.NewState}»";
            if (!string.IsNullOrWhiteSpace(history.Comment))
                desc += $", Комментарий: {history.Comment}";

            events.Add(new PatientTimelineEventDto(
                history.Id,
                $"Зуб №{history.ToothNumber}",
                desc,
                history.ChangedAt,
                "Icons.Material.Filled.HealthAndSafety",
                "Info"));
        }

        // 4. Attachments
        var files = await unitOfWork.PatientFiles.GetByPatientIdAsync(request.PatientId, cancellationToken);
        foreach (var file in files)
        {
            var desc = $"Файл: {file.FileName} ({FormatSize(file.SizeBytes)})";
            if (!string.IsNullOrWhiteSpace(file.Description))
                desc += $", Описание: {file.Description}";

            events.Add(new PatientTimelineEventDto(
                file.Id,
                "Загружен файл",
                desc,
                file.UploadedAt,
                "Icons.Material.Filled.AttachFile",
                "Secondary"));
        }

        var orderedEvents = events.OrderByDescending(e => e.Timestamp).ToList();
        return Result.Success<IReadOnlyList<PatientTimelineEventDto>>(orderedEvents);
    }

    private static string FormatSize(long bytes)
    {
        string[] suffix = ["B", "KB", "MB", "GB"];
        double doubleBytes = bytes;
        int i = 0;
        while (doubleBytes >= 1024 && i < suffix.Length - 1)
        {
            doubleBytes /= 1024;
            i++;
        }
        return $"{doubleBytes:N1} {suffix[i]}";
    }
}
