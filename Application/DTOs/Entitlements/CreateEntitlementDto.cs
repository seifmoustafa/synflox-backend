using System;
using Domain.Enums;

namespace Application.DTOs.Entitlements;

/// <summary>
/// Internal DTO for creating a new entitlement entity
/// Used by AutoMapper to create SubscriptionEntitlement
/// </summary>
public class CreateEntitlementDto
{
    public Guid SubscriptionId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ModuleId { get; set; }
    public EntitlementGrantType GrantType { get; set; }
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;
    public EntitlementSource Source { get; set; }
    public bool IsCustom { get; set; }
    public bool CanCreate { get; set; } = true;
    public bool CanRead { get; set; } = true;
    public bool CanUpdate { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool CanExport { get; set; } = true;
    public string? Features { get; set; }
    public string? UsageLimits { get; set; }
    public string? Icon { get; set; }
    public bool DisplayInMenu { get; set; } = true;
    public string? UpgradeCta { get; set; }
    public string? UpgradeUrl { get; set; }
    public Guid? GrantedByAdminId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
    
    /// <summary>
    /// Factory method for creating full project entitlement DTO
    /// </summary>
    public static CreateEntitlementDto ForFullProject(
        Guid subscriptionId, 
        Guid projectId, 
        EntitlementSource source,
        Guid? grantedByAdminId = null)
    {
        return new CreateEntitlementDto
        {
            SubscriptionId = subscriptionId,
            ProjectId = projectId,
            ModuleId = null,
            GrantType = EntitlementGrantType.FullProject,
            AccessLevel = EntitlementAccessLevel.Full,
            Source = source,
            IsCustom = source == EntitlementSource.AdminGrant,
            GrantedByAdminId = grantedByAdminId
        };
    }
    
    /// <summary>
    /// Factory method for creating module entitlement DTO
    /// </summary>
    public static CreateEntitlementDto ForModule(
        Guid subscriptionId, 
        Guid projectId, 
        Guid moduleId, 
        EntitlementSource source,
        Guid? grantedByAdminId = null)
    {
        return new CreateEntitlementDto
        {
            SubscriptionId = subscriptionId,
            ProjectId = projectId,
            ModuleId = moduleId,
            GrantType = EntitlementGrantType.SpecificModules,
            AccessLevel = EntitlementAccessLevel.Full,
            Source = source,
            IsCustom = source == EntitlementSource.AdminGrant,
            GrantedByAdminId = grantedByAdminId
        };
    }
    
    /// <summary>
    /// Factory method for creating standalone module entitlement DTO
    /// </summary>
    public static CreateEntitlementDto ForStandaloneModule(
        Guid subscriptionId, 
        Guid moduleId, 
        EntitlementSource source,
        Guid? grantedByAdminId = null)
    {
        return new CreateEntitlementDto
        {
            SubscriptionId = subscriptionId,
            ProjectId = null,
            ModuleId = moduleId,
            GrantType = EntitlementGrantType.StandaloneModule,
            AccessLevel = EntitlementAccessLevel.Full,
            Source = source,
            IsCustom = source == EntitlementSource.AdminGrant,
            GrantedByAdminId = grantedByAdminId
        };
    }
}
