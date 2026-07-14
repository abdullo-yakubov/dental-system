using DentalLocal.Domain.Common;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Entities;

/// <summary>Неудаляемая история изменений по конкретной поверхности зуба (см. п. 4.6 ТЗ).</summary>
public class ToothSurfaceHistory : Entity, IImmutableRecord
{
    public Guid ToothId { get; private set; }
    public ToothState Tooth { get; private set; } = null!;
    public string SurfaceCode { get; private set; } = null!;
    public ToothStateType Status { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Icd10Code { get; private set; }
    public string? Icd11Code { get; private set; }
    public Guid? VisitId { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public string? DoctorComment { get; private set; }

    private ToothSurfaceHistory()
    {
    }

    public static ToothSurfaceHistory Create(
        Guid toothId,
        string surfaceCode,
        ToothStateType status,
        string? diagnosis,
        string? icd10Code,
        string? icd11Code,
        Guid? visitId,
        string? doctorComment)
    {
        return new ToothSurfaceHistory
        {
            ToothId = toothId,
            SurfaceCode = surfaceCode,
            Status = status,
            Diagnosis = diagnosis,
            Icd10Code = icd10Code,
            Icd11Code = icd11Code,
            VisitId = visitId,
            DoctorComment = doctorComment,
            ChangedAt = DateTimeOffset.UtcNow,
        };
    }
}
