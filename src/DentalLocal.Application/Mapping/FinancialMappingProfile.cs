using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.Payments;
using DentalLocal.Shared.Enums;

namespace DentalLocal.Application.Mapping;

public sealed class FinancialMappingProfile : Profile
{
    public FinancialMappingProfile()
    {
        CreateMap<Payment, PaymentDto>()
            .ForCtorParam(nameof(PaymentDto.PatientFullName),
                opt => opt.MapFrom(p => p.Patient != null ? p.Patient.FullName : string.Empty))
            .ForCtorParam(nameof(PaymentDto.Method),
                opt => opt.MapFrom(p => (PaymentMethod)(short)p.Method))
            .ForCtorParam(nameof(PaymentDto.Kind),
                opt => opt.MapFrom(p => (PaymentKind)(short)p.Kind));

        CreateMap<Debt, DebtDto>()
            .ForCtorParam(nameof(DebtDto.PatientFullName),
                opt => opt.MapFrom(d => d.Patient != null ? d.Patient.FullName : string.Empty))
            .ForCtorParam(nameof(DebtDto.Status),
                opt => opt.MapFrom(d => (DebtStatus)(short)d.Status));

        CreateMap<Debt, DebtListItemDto>()
            .ForCtorParam(nameof(DebtListItemDto.PatientFullName),
                opt => opt.MapFrom(d => d.Patient != null ? d.Patient.FullName : string.Empty))
            .ForCtorParam(nameof(DebtListItemDto.PatientPhone),
                opt => opt.MapFrom(d => d.Patient != null ? d.Patient.Phone : string.Empty))
            .ForCtorParam(nameof(DebtListItemDto.PatientBirthDate),
                opt => opt.MapFrom(d => d.Patient != null ? d.Patient.BirthDate : default))
            .ForCtorParam(nameof(DebtListItemDto.Status),
                opt => opt.MapFrom(d => (DebtStatus)(short)d.Status))
            .ForCtorParam(nameof(DebtListItemDto.VisitDate),
                opt => opt.MapFrom(d => d.Visit != null ? d.Visit.VisitDate : default));
    }
}
