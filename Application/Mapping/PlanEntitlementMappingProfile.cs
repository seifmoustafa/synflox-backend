using Application.DTOs.PlanEntitlements;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for PlanEntitlement mappings
/// </summary>
public class PlanEntitlementMappingProfile : Profile
{
    public PlanEntitlementMappingProfile()
    {
        // ========== ID Decryption Mappings ==========
        
        // Decrypt PlanEntitlementId for single entitlement operations
        CreateMap<PlanEntitlementIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        // Decrypt PlanId for plan entitlement operations  
        CreateMap<PlanIdForEntitlementRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        
        // ========== Entity to DTO Mappings ==========
        CreateMap<PlanEntitlement, PlanEntitlementDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.PlanId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.PlanId))
            .ForMember(d => d.PlanName, opt => opt.MapFrom(s => s.Plan != null ? s.Plan.Name : string.Empty))
            .ForMember(d => d.ProjectId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ProjectId))
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s => s.Project != null ? s.Project.Name : null))
            .ForMember(d => d.ModuleId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ModuleId))
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.Module != null ? s.Module.Name : null))
            .ForMember(d => d.TargetType, opt => opt.MapFrom(s => s.TargetType))
            .ForMember(d => d.TargetName, opt => opt.MapFrom(s => s.TargetName))
            .ForMember(d => d.AccessLevelDisplay, opt => opt.MapFrom(s => GetAccessLevelDisplay(s.AccessLevel)))
            .ForMember(d => d.HasFullAccess, opt => opt.MapFrom(s => s.HasFullAccess))
            // Audit fields
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.IsActive))
            .ForMember(d => d.CreatedTimestamp, opt => opt.MapFrom(s => s.CreatedTimestamp))
            .ForMember(d => d.UpdatedTimestamp, opt => opt.MapFrom(s => s.UpdatedTimestamp))
            .ForMember(d => d.CreatedBy, opt => opt.MapFrom(s => s.CreatedBy.HasValue ? s.CreatedBy.Value.ToString() : null))
            .ForMember(d => d.UpdatedBy, opt => opt.MapFrom(s => s.UpdatedBy.HasValue ? s.UpdatedBy.Value.ToString() : null));
    }

    private static string GetAccessLevelDisplay(EntitlementAccessLevel level)
    {
        return level switch
        {
            EntitlementAccessLevel.Full => "Full Access",
            EntitlementAccessLevel.ReadOnly => "Read Only",
            EntitlementAccessLevel.ExportOnly => "Export Only",
            EntitlementAccessLevel.Blocked => "Blocked",
            _ => "Unknown"
        };
    }
}
