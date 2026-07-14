using AutoMapper;
using DentalLocal.Domain.Entities;
using DentalLocal.Shared.Contracts.Reminders;
using DentalLocal.Shared.Enums;

namespace DentalLocal.Application.Mapping;

public sealed class ReminderMappingProfile : Profile
{
    public ReminderMappingProfile()
    {
        CreateMap<Reminder, ReminderDto>()
            .ForCtorParam(nameof(ReminderDto.Type),
                opt => opt.MapFrom(r => (ReminderType)(short)r.Type));
    }
}
