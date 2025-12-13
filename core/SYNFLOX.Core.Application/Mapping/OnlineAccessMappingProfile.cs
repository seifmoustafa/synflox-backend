using AutoMapper;
using Domain.Entities.OnlineAccess;
using Application.DTOs.Common;
using Application.DTOs.OnlineAccess;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for Online Access entities and DTOs.
/// </summary>
public class OnlineAccessMappingProfile : Profile
{
    public OnlineAccessMappingProfile()
    {
        // ========== ID Decryption Mappings (Request DTOs → Guid) ==========
        CreateMap<SubscriptionIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        CreateMap<CompanyIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        CreateMap<TokenIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        CreateMap<DeviceIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        // ========== Response DTO Self-Mappings with Encryption ==========
        // These are used to encrypt IDs in manually constructed response DTOs
        
        CreateMap<RegisterDeviceResponse, RegisterDeviceResponse>()
            .ForMember(d => d.DeviceId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.DeviceId));
        
        CreateMap<PendingChangesDto, PendingChangesDto>()
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.SubscriptionId));
        
        CreateMap<OnlineSubscriptionStatusDto, OnlineSubscriptionStatusDto>()
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.SubscriptionId))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.PlanId));
        
        CreateMap<EntitlementMatrixDto, EntitlementMatrixDto>()
            .ForMember(d => d.Projects, opt => opt.MapFrom(s => s.Projects))
            .ForMember(d => d.Modules, opt => opt.MapFrom(s => s.Modules));
        
        CreateMap<ProjectAccessDto, ProjectAccessDto>()
            .ForMember(d => d.ProjectId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.ProjectId));
        
        CreateMap<ModuleAccessDto, ModuleAccessDto>()
            .ForMember(d => d.ModuleId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.ModuleId))
            .ForMember(d => d.ProjectId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ProjectId));
        
        CreateMap<OnlineTokenValidationDto, OnlineTokenValidationDto>()
            .ForMember(d => d.TokenId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.TokenId))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.CompanyId))
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.SubscriptionId));

        // ========== OnlineClientToken Mappings ==========
        CreateMap<OnlineClientToken, OnlineClientTokenDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.SubscriptionId))
            .ForMember(d => d.BoundDeviceCount, opt => opt.MapFrom(s => s.BoundDevices != null ? s.BoundDevices.Count : 0))
            .ForMember(d => d.IsValid, opt => opt.MapFrom(s => s.IsValid))
            .ForMember(d => d.IsExpired, opt => opt.MapFrom(s => s.IsExpired))
            .ForMember(d => d.DaysUntilExpiry, opt => opt.MapFrom(s => s.DaysUntilExpiry))
            .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Company != null ? s.Company.Name : null))
            .ForMember(d => d.SubscriptionPlanName, opt => opt.MapFrom(s => s.Subscription != null && s.Subscription.Plan != null ? s.Subscription.Plan.Name : null));

        // ========== OnlineDeviceBinding Mappings ==========
        CreateMap<OnlineDeviceBinding, OnlineDeviceDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.TokenId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.TokenId))
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.SubscriptionId));

        // ========== SubscriptionChangeLog Mappings ==========
        CreateMap<SubscriptionChangeLog, SubscriptionChangeLogDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.SubscriptionId))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.PlanId))
            .ForMember(d => d.DaysUntilEffective, opt => opt.MapFrom(s => s.DaysUntilEffective))
            .ForMember(d => d.IsReadyToApply, opt => opt.MapFrom(s => s.IsReadyToApply))
            .ForMember(d => d.SubscriptionName, opt => opt.MapFrom(s => s.Subscription != null ? s.Subscription.Company != null ? s.Subscription.Company.Name : null : null))
            .ForMember(d => d.PlanName, opt => opt.MapFrom(s => s.Plan != null ? s.Plan.Name : null));
    }
}
