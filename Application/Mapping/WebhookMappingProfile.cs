using AutoMapper;
using Domain.Entities.Webhooks;
using Application.DTOs.Webhooks;
using Application.Services;
using System.Text.Json;

namespace Application.Mapping;

public class WebhookMappingProfile : Profile
{
    public WebhookMappingProfile()
    {
        CreateMap<Webhook, WebhookDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.Events,
                opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.Events)
                    ? JsonSerializer.Deserialize<Domain.Enums.WebhookEventType[]>(s.Events, new JsonSerializerOptions())
                    : Array.Empty<Domain.Enums.WebhookEventType>()));

        CreateMap<CreateWebhookRequest, Webhook>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.Events,
                opt => opt.MapFrom(s => s.Events != null && s.Events.Length > 0
                    ? JsonSerializer.Serialize(s.Events, new JsonSerializerOptions())
                    : "[]")) // Return empty JSON array instead of null
            .ForMember(d => d.Secret, opt => opt.MapFrom(s => !string.IsNullOrWhiteSpace(s.Secret) ? s.Secret : string.Empty)) // Use provided secret or empty string (service will generate if empty)
            .ForMember(d => d.RetryCount, opt => opt.MapFrom(s => s.RetryCount > 0 ? s.RetryCount : 3))
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.IsActive))
            .ForMember(d => d.LastTriggeredAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<WebhookDelivery, WebhookDeliveryDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.WebhookId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.WebhookId));
    }
}

