using AutoMapper;
using Domain.Entities.Notifications;
using Application.DTOs.Notifications;
using Application.Services;

namespace Application.Mapping;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.TypeName,
                opt => opt.MapFrom(s => s.Type.ToString()));
    }
}

