using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.Materials;

namespace DentalLocal.Application.Mapping;

public sealed class MaterialMappingProfile : Profile
{
    public MaterialMappingProfile()
    {
        CreateMap<Material, MaterialDto>();

        CreateMap<MaterialUsage, MaterialUsageDto>()
            .ForCtorParam(nameof(MaterialUsageDto.MaterialName),
                opt => opt.MapFrom(mu => mu.Material != null ? mu.Material.Name : string.Empty));
    }
}
