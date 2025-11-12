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
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Whether this subscription should auto-renew
    /// Can override plan's default AutoRenew setting
    /// </summary>
    public bool AutoRenew { get; set; }

    /// <summary>
    /// Optional override for upgrade policy (null = use plan's policy)
    /// </summary>
    public UpgradePolicy? UpgradePolicyOverride { get; set; }

    /// <summary>
    /// Scheduled next plan for deferred upgrades
    /// </summary>
    public Guid? NextPlanId { get; set; }

    /// <summary>
    /// When the next plan should activate (UTC)
    /// </summary>
    public DateTime? NextPlanStartDateUtc { get; set; }

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

    // Navigation properties
    public Company Company { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
    public SubscriptionPlan? NextPlan { get; set; }
    public Subscription? ParentSubscription { get; set; }
}
