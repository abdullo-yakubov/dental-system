using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.Procedures;

namespace DentalLocal.Application.Mapping;

public sealed class ProcedureMappingProfile : Profile
{
    public ProcedureMappingProfile()
    {
        CreateMap<ProcedureCategory, ProcedureCategoryDto>()
            .ForCtorParam(nameof(ProcedureCategoryDto.Name), opt => opt.MapFrom(c => c.Name))
            .ForCtorParam(nameof(ProcedureCategoryDto.Description), opt => opt.MapFrom(_ => (string?)null))
            .ForCtorParam(nameof(ProcedureCategoryDto.Color), opt => opt.MapFrom(c => c.Color));

        CreateMap<Procedure, ProcedureDto>()
            .ForCtorParam(nameof(ProcedureDto.CategoryName),
                opt => opt.MapFrom(p => p.Category != null ? p.Category.Name : string.Empty))
            .ForCtorParam(nameof(ProcedureDto.DurationMinutes),
                opt => opt.MapFrom(p => (int?)p.DurationMinutes))
            .ForCtorParam(nameof(ProcedureDto.Color),
                opt => opt.MapFrom(p => p.Category != null ? p.Category.Color : null));
    }
}
