using System.Text.Json;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Enums;
using Application.DTOs.Entitlements;

// Needed for Subscription entity mapping
using Subscription = Domain.Entities.Subscriptions.Subscription;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for entitlement-related mappings
/// </summary>
public class EntitlementMappingProfile : Profile
{
    public EntitlementMappingProfile()
    {
        // ========== SubscriptionEntitlement → SubscriptionEntitlementDto ==========
        CreateMap<SubscriptionEntitlement, SubscriptionEntitlementDto>()
            .ForMember(d => d.Id, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.SubscriptionId))
            .ForMember(d => d.ProjectId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ProjectId))
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s => s.Project != null ? s.Project.Name : null))
            .ForMember(d => d.ModuleId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ModuleId))
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.Module != null ? s.Module.Name : null))
            .ForMember(d => d.GrantedByAdminId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.GrantedByAdminId))
            .ForMember(d => d.GrantedByAdminName, opt => opt.MapFrom(s => s.GrantedByAdmin != null ? $"{s.GrantedByAdmin.FirstName} {s.GrantedByAdmin.LastName}".Trim() : null))
            // Parse JSON fields
            .ForMember(d => d.Features, opt => opt.MapFrom(s => ParseFeatures(s.Features)))
            .ForMember(d => d.UsageLimits, opt => opt.MapFrom(s => ParseUsageLimits(s.UsageLimits)));

        // ========== SubscriptionEntitlement → ProjectEntitlementDto ==========
        CreateMap<SubscriptionEntitlement, ProjectEntitlementDto>()
            .ForMember(d => d.ProjectId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.ProjectId!.Value))
            .ForMember(d => d.ProjectName, opt => opt.MapFrom(s => s.Project != null ? s.Project.Name : string.Empty))
            .ForMember(d => d.ProjectDescription, opt => opt.MapFrom(s => s.Project != null ? s.Project.Description : null))
            .ForMember(d => d.Icon, opt => opt.MapFrom(s => s.Icon))
            .ForMember(d => d.AccessLevel, opt => opt.MapFrom(s => s.AccessLevel))
            .ForMember(d => d.GrantType, opt => opt.MapFrom(s => s.GrantType))
            .ForMember(d => d.AllowedOperations, opt => opt.MapFrom(s => GetAllowedOperations(s)))
            .ForMember(d => d.Features, opt => opt.MapFrom(s => ParseFeatures(s.Features)))
            .ForMember(d => d.ExpiresAt, opt => opt.MapFrom(s => s.ExpiresAt))
            .ForMember(d => d.DisplayInMenu, opt => opt.MapFrom(s => s.DisplayInMenu))
            .ForMember(d => d.Modules, opt => opt.Ignore()); // Set by service

        // ========== SubscriptionEntitlement → ModuleEntitlementDto ==========
        CreateMap<SubscriptionEntitlement, ModuleEntitlementDto>()
            .ForMember(d => d.ModuleId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.ModuleId!.Value))
            .ForMember(d => d.ModuleName, opt => opt.MapFrom(s => s.Module != null ? s.Module.Name : string.Empty))
            .ForMember(d => d.ModuleDescription, opt => opt.MapFrom(s => s.Module != null ? s.Module.Description : null))
            .ForMember(d => d.Icon, opt => opt.MapFrom(s => s.Icon))
            .ForMember(d => d.ProjectId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid?>(s => s.ProjectId))
            .ForMember(d => d.AccessLevel, opt => opt.MapFrom(s => s.AccessLevel))
            .ForMember(d => d.AllowedOperations, opt => opt.MapFrom(s => GetAllowedOperations(s)))
            .ForMember(d => d.Features, opt => opt.MapFrom(s => ParseFeatures(s.Features)))
            .ForMember(d => d.UsageLimits, opt => opt.MapFrom(s => ParseUsageLimits(s.UsageLimits)))
            .ForMember(d => d.ExpiresAt, opt => opt.MapFrom(s => s.ExpiresAt))
            .ForMember(d => d.DisplayInMenu, opt => opt.MapFrom(s => s.DisplayInMenu));

        // ========== CreateEntitlementDto → SubscriptionEntitlement ==========
        CreateMap<CreateEntitlementDto, SubscriptionEntitlement>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(d => d.IsDeleted, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.CreatedTimestamp, opt => opt.MapFrom(_ => DateTime.UtcNow)) // UTC time
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.CreatedBy, opt => opt.Ignore())
            .ForMember(d => d.UpdatedBy, opt => opt.Ignore())
            .ForMember(d => d.DeletedBy, opt => opt.Ignore())
            // Navigation properties - ignore, set by EF
            .ForMember(d => d.Subscription, opt => opt.Ignore())
            .ForMember(d => d.Project, opt => opt.Ignore())
            .ForMember(d => d.Module, opt => opt.Ignore())
            .ForMember(d => d.GrantedByAdmin, opt => opt.Ignore());

        // ========== SubscriptionEntitlement → CreateEntitlementDto (for cloning) ==========
        CreateMap<SubscriptionEntitlement, CreateEntitlementDto>();

        // ========== Subscription → EntitlementMatrixDto ==========
        CreateMap<Subscription, EntitlementMatrixDto>()
            .ForMember(d => d.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.Version, opt => opt.MapFrom(s => s.EntitlementsVersion))
            .ForMember(d => d.ExpiryDateUtc, opt => opt.MapFrom(s => s.ExpiryDateUtc))
            .ForMember(d => d.ExportDeadlineUtc, opt => opt.MapFrom(s => s.ExportDeadlineUtc))
            .ForMember(d => d.AccessRestrictionMessage, opt => opt.MapFrom(s => s.AccessRestrictionMessage))
            .ForMember(d => d.AccessMode, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.Projects, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.StandaloneModules, opt => opt.Ignore()) // Set by service
            .ForMember(d => d.GlobalUsageLimits, opt => opt.Ignore())
            .ForMember(d => d.AvailableUpgrades, opt => opt.Ignore())
            .ForMember(d => d.GeneratedAtUtc, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.CacheTtlSeconds, opt => opt.MapFrom(_ => 86400))
            .ForMember(d => d.MenuConfig, opt => opt.MapFrom(s => s.Plan));

        // ========== SubscriptionPlan → MenuConfigDto ==========
        CreateMap<SubscriptionPlan, MenuConfigDto>()
            .ForMember(d => d.ShowLockedItems, opt => opt.MapFrom(s => s.ShowLockedModulesInMenu))
            .ForMember(d => d.LockedItemStyle, opt => opt.MapFrom(s => s.LockedItemStyle ?? "greyed_with_lock"))
            .ForMember(d => d.DefaultUpgradeCta, opt => opt.MapFrom(_ => "Upgrade"))
            .ForMember(d => d.DefaultUpgradeUrl, opt => opt.MapFrom(_ => "/upgrade"));

        // ========== Request DTOs → Single Guid decryption ==========
        // These are used to decrypt the primary ID from request DTOs
        // Service calls: var decryptedId = _mapper.Map<Guid>(request)
        //
        // For complex request → CreateEntitlementDto mapping,
        // use CreateEntitlementDto.Factory methods in service layer
        // This follows SYNFLOX ID Encryption Rule: decryption happens via AutoMapper
        // Decrypt SubscriptionId from GrantEntitlementRequest
        CreateMap<GrantEntitlementRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Decrypt EntitlementId from UpdateEntitlementRequest
        CreateMap<UpdateEntitlementRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Decrypt EntitlementId from RevokeEntitlementRequest
        CreateMap<RevokeEntitlementRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Decrypt SubscriptionId from AccessCheckRequest
        CreateMap<AccessCheckRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Decrypt SubscriptionId from BulkAccessCheckRequest
        CreateMap<BulkAccessCheckRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Decrypt SubscriptionId from GrantProjectAccessRequest
        CreateMap<GrantProjectAccessRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Decrypt SubscriptionId from GrantModuleAccessRequest
        CreateMap<GrantModuleAccessRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
            
        // Decrypt SubscriptionId from GrantStandaloneModuleRequest
        CreateMap<GrantStandaloneModuleRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
    }

    #region Helper Methods

    private static List<string> ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrEmpty(featuresJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(featuresJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static Dictionary<string, object> ParseUsageLimits(string? limitsJson)
    {
        if (string.IsNullOrEmpty(limitsJson))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(limitsJson) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    private static List<string> GetAllowedOperations(SubscriptionEntitlement e)
    {
        var ops = new List<string>();
        if (e.CanRead) ops.Add("GET");
        if (e.CanCreate) ops.Add("POST");
        if (e.CanUpdate) ops.Add("PUT");
        if (e.CanDelete) ops.Add("DELETE");
        if (e.CanExport) ops.Add("EXPORT");
        return ops;
    }

    #endregion
}
