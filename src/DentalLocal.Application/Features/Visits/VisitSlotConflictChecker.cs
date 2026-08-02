using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Repositories;

namespace DentalLocal.Application.Features.Visits;

/// <summary>
/// Жёсткая проверка пересечения слотов — двух пациентов на одно и то же время у одного врача
/// записать нельзя (см. промпт 46 аудита зависаний; в отличие от предупреждения о работе вне
/// графика в CreateVisitCommandHandler.GetScheduleWarningAsync, это блокировка, а не совет).
/// Общая для создания визита и переноса (в т.ч. drag-and-drop в календаре).
/// </summary>
public static class VisitSlotConflictChecker
{
    public static async Task<bool> HasConflictAsync(
        IUnitOfWork unitOfWork,
        string doctorName,
        DateOnly visitDate,
        TimeOnly visitTime,
        Guid? excludeVisitId,
        CancellationToken cancellationToken)
    {
        var sameDayVisits = await unitOfWork.Visits.GetByDateRangeAsync(visitDate, visitDate, cancellationToken);

        return sameDayVisits.Any(v =>
            v.Id != excludeVisitId
            && v.Status != VisitStatus.Cancelled
            && v.DoctorName == doctorName
            && v.VisitTime == visitTime);
    }
}
