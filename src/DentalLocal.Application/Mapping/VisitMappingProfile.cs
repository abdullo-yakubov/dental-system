using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.Visits;
using DentalLocal.Shared.Enums;

namespace DentalLocal.Application.Mapping;

public sealed class VisitMappingProfile : Profile
{
    public VisitMappingProfile()
    {
        CreateMap<TreatmentItem, TreatmentItemDto>()
            .ForCtorParam(nameof(TreatmentItemDto.ProcedureName),
                opt => opt.MapFrom(i => i.Procedure != null ? i.Procedure.Name : string.Empty));

        CreateMap<Visit, VisitDto>()
            .ForCtorParam(nameof(VisitDto.PatientFullName),
                opt => opt.MapFrom(v => v.Patient != null ? v.Patient.FullName : string.Empty))
            .ForCtorParam(nameof(VisitDto.Status),
                opt => opt.MapFrom(v => (VisitStatus)(short)v.Status))
            .ForCtorParam(nameof(VisitDto.Items),
                opt => opt.MapFrom(v => v.Items))
            .ForCtorParam(nameof(VisitDto.RemainingAmount),
                opt => opt.MapFrom(v => v.RemainingAmount))
            .ForCtorParam(nameof(VisitDto.CreatedAt),
                opt => opt.MapFrom(v => v.CreatedAt))
            .ForCtorParam(nameof(VisitDto.UpdatedAt),
                opt => opt.MapFrom(v => v.UpdatedAt ?? v.CreatedAt));

        CreateMap<Visit, VisitListItemDto>()
            .ForCtorParam(nameof(VisitListItemDto.PatientFullName),
                opt => opt.MapFrom(v => v.Patient != null ? v.Patient.FullName : string.Empty))
            .ForCtorParam(nameof(VisitListItemDto.Status),
                opt => opt.MapFrom(v => (VisitStatus)(short)v.Status))
            .ForCtorParam(nameof(VisitListItemDto.RemainingAmount),
                opt => opt.MapFrom(v => v.RemainingAmount))
            .ForCtorParam(nameof(VisitListItemDto.ProcedureCount),
                opt => opt.MapFrom(v => v.Items.Count));
    }
}
