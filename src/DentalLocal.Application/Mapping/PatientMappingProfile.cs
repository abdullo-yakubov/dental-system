using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.Patients;

namespace DentalLocal.Application.Mapping;

public sealed class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        var today = () => DateOnly.FromDateTime(DateTime.UtcNow);

        CreateMap<Patient, PatientDto>()
            .ForCtorParam(nameof(PatientDto.CardNumberDisplay), opt => opt.MapFrom(p => p.CardNumberDisplay))
            .ForCtorParam(nameof(PatientDto.FullName), opt => opt.MapFrom(p => p.FullName))
            .ForCtorParam(nameof(PatientDto.Age), opt => opt.MapFrom(p => p.GetAge(today())));

        CreateMap<Patient, PatientListItemDto>()
            .ForCtorParam(nameof(PatientListItemDto.CardNumberDisplay), opt => opt.MapFrom(p => p.CardNumberDisplay))
            .ForCtorParam(nameof(PatientListItemDto.FullName), opt => opt.MapFrom(p => p.FullName))
            .ForCtorParam(nameof(PatientListItemDto.Age), opt => opt.MapFrom(p => p.GetAge(today())));
        CreateMap<PatientFile, PatientFileDto>()
            .ForCtorParam(nameof(PatientFileDto.FileType), opt => opt.MapFrom(pf => (short)pf.FileType))
            .ForCtorParam(nameof(PatientFileDto.BeforeAfterTag), opt => opt.MapFrom(pf => pf.BeforeAfterType));
    }
}
