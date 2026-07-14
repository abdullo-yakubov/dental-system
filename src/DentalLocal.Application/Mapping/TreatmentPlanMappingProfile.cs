using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.TreatmentPlans;

namespace DentalLocal.Application.Mapping;

public sealed class TreatmentPlanMappingProfile : Profile
{
    public TreatmentPlanMappingProfile()
    {
        CreateMap<TreatmentPlanItem, TreatmentPlanItemDto>()
            .ForCtorParam(nameof(TreatmentPlanItemDto.ProcedureName),
                opt => opt.MapFrom(i => i.Procedure != null ? i.Procedure.Name : string.Empty));

        CreateMap<TreatmentPlan, TreatmentPlanDto>()
            .ForCtorParam(nameof(TreatmentPlanDto.TotalEstimatedPrice), opt => opt.MapFrom(t => t.TotalEstimatedPrice))
            .ForCtorParam(nameof(TreatmentPlanDto.CompletedCount), opt => opt.MapFrom(t => t.CompletedCount))
            .ForCtorParam(nameof(TreatmentPlanDto.Items), opt => opt.MapFrom(t => t.Items.OrderBy(i => i.OrderIndex)));
    }
}
