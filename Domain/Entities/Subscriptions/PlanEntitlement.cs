using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Defines access rights at the PLAN level.
/// All subscriptions of the same plan automatically inherit these entitlements.
/// This replaces the old subscription-level SubscriptionEntitlement entity.
/// </summary>
public class PlanEntitlement : AuditEntity<Guid>
{
    #region Relationships

    /// <summary>
    /// The plan this entitlement belongs to
    /// </summary>
    public Guid PlanId { get; set; }
    public virtual SubscriptionPlan Plan { get; set; } = null!;

    /// <summary>
    /// Target Project (if this is a project-level entitlement)
    /// Either ProjectId OR ModuleId must be set, not both
    /// </summary>
    public Guid? ProjectId { get; set; }
    public virtual Project? Project { get; set; }

    /// <summary>
    /// Target Module (if this is a module-level entitlement)
    /// Either ProjectId OR ModuleId must be set, not both
    /// </summary>
    public Guid? ModuleId { get; set; }
    public virtual Module? Module { get; set; }

    #endregion

    #region Access Configuration

    /// <summary>
    /// Overall access level for this entitlement
    /// </summary>
    public EntitlementAccessLevel AccessLevel { get; set; } = EntitlementAccessLevel.Full;

    /// <summary>
    /// Can create new records
    /// </summary>
    public bool CanCreate { get; set; } = true;

    /// <summary>
    /// Can read/view records
    /// </summary>
    public bool CanRead { get; set; } = true;

    /// <summary>
    /// Can update existing records
    /// </summary>
    public bool CanUpdate { get; set; } = true;

    /// <summary>
    /// Can delete records
    /// </summary>
    public bool CanDelete { get; set; } = true;

    /// <summary>
    /// Can export data
    /// </summary>
    public bool CanExport { get; set; } = true;

    /// <summary>
    /// Whether to display this item in client menus
    /// </summary>
    public bool DisplayInMenu { get; set; } = true;

    /// <summary>
    /// Optional features for this entitlement (comma-separated)
    /// Example: "reports,analytics,advanced-search"
    /// </summary>
    [StringLength(500)]
    public string? Features { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Type of target (Project or Module)
    /// </summary>
    public string TargetType => ProjectId.HasValue ? "Project" : "Module";

    /// <summary>
    /// Name of the target (Project or Module name)
    /// </summary>
    public string TargetName => Project?.Name ?? Module?.Name ?? "Unknown";

    /// <summary>
    /// Returns true if this grants full CRUD access
    /// </summary>
    public bool HasFullAccess => CanCreate && CanRead && CanUpdate && CanDelete && CanExport;

    #endregion
}
