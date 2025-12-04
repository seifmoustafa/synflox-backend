using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Enums;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Represents a concrete subscription instance for a company
/// Defines one entitlement window with start/expiry dates
/// </summary>
public class Subscription : AuditEntity<Guid>
{
    /// <summary>
    /// The company (tenant) that owns this subscription
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// The plan this subscription is based on
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// When this subscription window starts (UTC)
    /// </summary>
    public DateTime StartDateUtc { get; set; }

    /// <summary>
    /// When this subscription window expires (UTC)
    /// </summary>
    public DateTime ExpiryDateUtc { get; set; }

    /// <summary>
    /// Whether this subscription is currently active
    /// False when suspended, canceled, or expired
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this is a trial subscription
    /// </summary>
    public bool IsTrial { get; set; }

    /// <summary>
    /// Whether this subscription has expired (past expiry + grace)
    /// Set by background job
    /// Note: Lifetime subscriptions never expire naturally (IsExpired stays false unless manually canceled)
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Indicates if this is a lifetime/permanent subscription
    /// Lifetime subscriptions have ExpiryDateUtc = DateTime.MaxValue
    /// </summary>
    public bool IsLifetime => ExpiryDateUtc >= DateTime.MaxValue.AddYears(-1); // Close to max value

    /// <summary>
    /// Whether this subscription should auto-renew
    /// Can override plan's default AutoRenew setting
    /// Note: Lifetime subscriptions cannot auto-renew (always false)
    /// </summary>
    public bool AutoRenew { get; set; }

    #region Pause State (Timer Freeze)

    /// <summary>
    /// Whether this subscription is currently paused (timer frozen)
    /// Different from Suspended: Pause freezes time, Suspend just blocks access
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// When the subscription was paused (UTC)
    /// Used to calculate how long it was paused when unpausing
    /// </summary>
    public DateTime? PausedAtUtc { get; set; }

    /// <summary>
    /// Days remaining when pause was initiated
    /// Stored for easy restoration when unpausing
    /// </summary>
    public int? RemainingDaysWhenPaused { get; set; }

    #endregion

    /// <summary>
    /// Scheduled next subscription for deferred upgrades.
    /// When upgrading, a new subscription is created in "Scheduled" state and linked here.
    /// The background job activates it on the scheduled date.
    /// </summary>
    public Guid? NextSubscriptionId { get; set; }

    /// <summary>
    /// When the next subscription should activate (UTC)
    /// </summary>
    public DateTime? NextSubscriptionActivationDateUtc { get; set; }

    /// <summary>
    /// Parent subscription if this was created from an upgrade
    /// Tracks lineage
    /// </summary>
    public Guid? ParentSubscriptionId { get; set; }

    /// <summary>
    /// Human-readable reason for last status change
    /// Examples: "Expired", "Suspended by admin", "Upgraded (Prorated)"
    /// </summary>
    [StringLength(500)]
    public string? StatusReason { get; set; }

    /// <summary>
    /// Currency used for this subscription
    /// </summary>
    public Currency Currency { get; set; }

    /// <summary>
    /// Amount paid/billed for this subscription in the specified currency
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Encrypted offline license key for this specific subscription
    /// Contains: CompanyId, PlanId, ExpiryDate, Features, Modules, Signature
    /// Used by client applications for offline validation
    /// </summary>
    [StringLength(2000)]
    public string? OfflineLicenseKey { get; set; }

    /// <summary>
    /// When the offline license key was generated (UTC)
    /// </summary>
    public DateTime? LicenseKeyGeneratedAt { get; set; }

    /// <summary>
    /// Version of the license key format for future compatibility
    /// </summary>
    public int LicenseKeyVersion { get; set; } = 1;

    #region Access Control (Enterprise Entitlement System)

    /// <summary>
    /// Current access mode for this subscription
    /// Determines global access level (Full, GracePeriod, ReadOnly, ExportOnly, Blocked)
    /// Updated by background job based on subscription state
    /// </summary>
    public SubscriptionAccessMode AccessMode { get; set; } = SubscriptionAccessMode.Full;

    /// <summary>
    /// When export-only mode ends (after which access is Blocked)
    /// Set when subscription transitions to ExportOnly mode
    /// </summary>
    public DateTime? ExportDeadlineUtc { get; set; }

    /// <summary>
    /// Version number for entitlements, incremented on any entitlement change
    /// Used by client applications to detect when to refresh cached entitlements
    /// </summary>
    public int EntitlementsVersion { get; set; } = 1;

    /// <summary>
    /// Custom message to show when access is restricted
    /// Displayed to end users when they try to access blocked features
    /// </summary>
    [StringLength(500)]
    public string? AccessRestrictionMessage { get; set; }

    #endregion

    // Navigation properties
    public Company Company { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
    
    /// <summary>
    /// The scheduled next subscription (for deferred upgrades)
    /// </summary>
    public Subscription? NextSubscription { get; set; }
    
    /// <summary>
    /// Parent subscription if this was created from an upgrade/renewal
    /// </summary>
    public Subscription? ParentSubscription { get; set; }
    
    // SubscriptionEntitlements REMOVED - v2.0: Entitlements are now at Plan level (PlanEntitlement)
    // Access is determined by subscription.Plan.Entitlements
}
