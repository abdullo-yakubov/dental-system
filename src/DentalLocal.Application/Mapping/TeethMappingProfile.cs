using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.Teeth;
using DentalLocal.Shared.Enums;

namespace DentalLocal.Application.Mapping;

public sealed class TeethMappingProfile : Profile
{
    public TeethMappingProfile()
    {
        CreateMap<ToothState, ToothStateDto>()
            .ForCtorParam(nameof(ToothStateDto.State),
                opt => opt.MapFrom(t => (ToothStateType)(short)t.State))
            .ForCtorParam(nameof(ToothStateDto.UpdatedAt),
                opt => opt.MapFrom(t => t.UpdatedAt ?? t.CreatedAt));

        CreateMap<ToothStateHistory, ToothStateHistoryDto>()
            .ForCtorParam(nameof(ToothStateHistoryDto.OldState),
                opt => opt.MapFrom(h => (ToothStateType)(short)h.OldState))
            .ForCtorParam(nameof(ToothStateHistoryDto.NewState),
                opt => opt.MapFrom(h => (ToothStateType)(short)h.NewState));
    }
}
