using Application.DTOs.Notification;
using AutoMapper;
using Domain.Entities.Notifications;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for notification entities
/// </summary>
public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        // Notification -> NotificationDto
        CreateMap<Notification, NotificationDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => src.ReadAtUtc));

        // NotificationPreference -> NotificationPreferenceDto
        CreateMap<NotificationPreference, NotificationPreferenceDto>()
            .ForMember(dest => dest.QuietHoursStart, opt => opt.MapFrom(src => src.QuietHoursStart))
            .ForMember(dest => dest.QuietHoursEnd, opt => opt.MapFrom(src => src.QuietHoursEnd));

        // NotificationPreferenceDto -> NotificationPreference (for updates)
        CreateMap<NotificationPreferenceDto, NotificationPreference>()
            .ForMember(dest => dest.QuietHoursStart, opt => opt.Ignore())
            .ForMember(dest => dest.QuietHoursEnd, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.UserType, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Notes, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.SmsEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionNotifications, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityNotifications, opt => opt.Ignore())
            .ForMember(dest => dest.SystemNotifications, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionalNotifications, opt => opt.Ignore())
            .ForMember(dest => dest.DeviceNotifications, opt => opt.Ignore())
            .ForMember(dest => dest.Timezone, opt => opt.Ignore())
            .ForMember(dest => dest.EmailDigestFrequency, opt => opt.Ignore());
    }
}
