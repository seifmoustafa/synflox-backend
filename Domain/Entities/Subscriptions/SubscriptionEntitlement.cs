using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Represents a granular entitlement record for a subscription
/// Defines exactly what a subscription can access at project/module/feature level
/// </summary>
public class SubscriptionEntitlement : AuditEntity<Guid>
{
    #region Core References

    /// <summary>
    /// The subscription this entitlement belongs to
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Project being granted access to (null for standalone module or feature)
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Module being granted access to (null if granting full project access)
    /// </summary>
    public Guid? ModuleId { get; set; }

    #endregion

    #region Grant Configuration

    /// <summary>
    /// How this entitlement was granted (FullProject, SpecificModules, etc.)
    /// </summary>
    public EntitlementGrantType GrantType { get; set; }

    /// <summary>
    /// Access level for this entitlement (Full, ReadOnly, ExportOnly, etc.)
    /// </summary>
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;

    /// <summary>
    /// Source of this entitlement for audit trail (PlanDefault, AdminGrant, Upgrade, etc.)
    /// </summary>
    public EntitlementSource Source { get; set; }

    /// <summary>
    /// Whether this entitlement differs from the plan default
    /// True for custom grants by admin
    /// </summary>
    public bool IsCustom { get; set; }

    #endregion

    #region Operations & Features

    /// <summary>
    /// Can create new records in the entitled resource
    /// </summary>
    public bool CanCreate { get; set; } = true;

    /// <summary>
    /// Can read/view records in the entitled resource
    /// </summary>
    public bool CanRead { get; set; } = true;

    /// <summary>
    /// Can update/edit records in the entitled resource
    /// </summary>
    public bool CanUpdate { get; set; } = true;

    /// <summary>
    /// Can delete records in the entitled resource
    /// </summary>
    public bool CanDelete { get; set; } = true;

    /// <summary>
    /// Can export data from the entitled resource
    /// </summary>
    public bool CanExport { get; set; } = true;

    /// <summary>
    /// Specific feature flags within the module (JSON array)
    /// null = all features, ["payroll", "leave"] = only these features
    /// </summary>
    [StringLength(2000)]
    public string? Features { get; set; }

    /// <summary>
    /// Usage limits for metered features (JSON object)
    /// Example: {"apiCalls": 1000, "storage": "1GB", "users": 5}
    /// </summary>
    [StringLength(2000)]
    public string? UsageLimits { get; set; }

    #endregion

    #region Display & Marketing

    /// <summary>
    /// Icon name for UI display (e.g., "calculator", "users", "package")
    /// Used when showing this entitlement in client applications
    /// </summary>
    [StringLength(100)]
    public string? Icon { get; set; }

    /// <summary>
    /// Whether this item should be displayed in menu (for locked items shown for marketing)
    /// </summary>
    public bool DisplayInMenu { get; set; } = true;

    /// <summary>
    /// Custom upgrade call-to-action text (e.g., "Upgrade to Enterprise")
    /// Used when showing locked items for marketing
    /// </summary>
    [StringLength(200)]
    public string? UpgradeCta { get; set; }

    /// <summary>
    /// Custom upgrade URL (e.g., "/upgrade?module=accounting")
    /// </summary>
    [StringLength(500)]
    public string? UpgradeUrl { get; set; }

    #endregion

    #region Audit & Expiry

    /// <summary>
    /// Admin who granted this entitlement (if manually granted)
    /// </summary>
    public Guid? GrantedByAdminId { get; set; }

    /// <summary>
    /// Optional expiry date for this specific entitlement
    /// null = follows subscription expiry
    /// Used for time-limited promotional access or module trials
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Admin notes for audit purposes
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this entitlement is currently active
    /// Can be deactivated without deletion for temporary suspension
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Soft delete flag (inherited from AuditEntity via base)
    /// </summary>
    public bool IsDeleted { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// The subscription this entitlement belongs to
    /// </summary>
    public Subscription Subscription { get; set; } = null!;

    /// <summary>
    /// The project this entitlement grants access to (if applicable)
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// The module this entitlement grants access to (if applicable)
    /// </summary>
    public Module? Module { get; set; }

    /// <summary>
    /// The admin who granted this entitlement (if manually granted)
    /// </summary>
    public Admin? GrantedByAdmin { get; set; }

    #endregion
}
