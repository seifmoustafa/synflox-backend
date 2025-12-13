using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Entities.OnlineAccess;

/// <summary>
/// Tracks pending plan/subscription changes that will take effect at a future date.
/// Used for billing cycle-aware change management.
/// 
/// Examples:
/// - Price increase scheduled for next billing cycle
/// - Module removal pending until subscription renewal
/// - Device limit decrease waiting for current devices to be removed
/// </summary>
public class SubscriptionChangeLog : AuditEntity<Guid>
{
    /// <summary>
    /// The subscription this change applies to.
    /// </summary>
    public Guid SubscriptionId { get; set; }
    
    /// <summary>
    /// The plan ID if this is a plan-level change (affects all subscriptions).
    /// </summary>
    public Guid? PlanId { get; set; }
    
    /// <summary>
    /// Type of change (PriceChange, FeatureAdded, FeatureRemoved, etc.).
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string ChangeType { get; set; }
    
    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string ChangeDescription { get; set; }
    
    /// <summary>
    /// The previous value (JSON serialized if complex).
    /// </summary>
    [StringLength(2000)]
    public string? OldValue { get; set; }
    
    /// <summary>
    /// The new value (JSON serialized if complex).
    /// </summary>
    [StringLength(2000)]
    public string? NewValue { get; set; }
    
    /// <summary>
    /// When this change takes effect policy.
    /// </summary>
    public ChangeEffectPolicy EffectPolicy { get; set; }
    
    /// <summary>
    /// The specific date when this change will be applied.
    /// </summary>
    public DateTime EffectiveDateUtc { get; set; }
    
    /// <summary>
    /// Whether the change has been applied.
    /// </summary>
    public bool IsApplied { get; set; } = false;
    
    /// <summary>
    /// When the change was applied (if applied).
    /// </summary>
    public DateTime? AppliedAtUtc { get; set; }
    
    /// <summary>
    /// Who/what applied the change (e.g., "System", "Admin: john@example.com").
    /// </summary>
    [StringLength(200)]
    public string? AppliedBy { get; set; }
    
    /// <summary>
    /// Whether the customer was notified about this pending change.
    /// </summary>
    public bool CustomerNotified { get; set; } = false;
    
    /// <summary>
    /// When the customer was notified.
    /// </summary>
    public DateTime? NotifiedAtUtc { get; set; }
    
    /// <summary>
    /// Whether this change was cancelled before taking effect.
    /// </summary>
    public bool IsCancelled { get; set; } = false;
    
    /// <summary>
    /// Reason for cancellation if cancelled.
    /// </summary>
    [StringLength(500)]
    public string? CancellationReason { get; set; }
    
    // ========== Computed Properties ==========
    
    /// <summary>
    /// Days until this change takes effect.
    /// </summary>
    public int DaysUntilEffective => 
        IsApplied || IsCancelled ? 0 : 
        Math.Max(0, (int)(EffectiveDateUtc - DateTime.UtcNow).TotalDays);
    
    /// <summary>
    /// Whether the change is ready to be applied.
    /// </summary>
    public bool IsReadyToApply => 
        !IsApplied && 
        !IsCancelled && 
        DateTime.UtcNow >= EffectiveDateUtc;
    
    // ========== Navigation Properties ==========
    
    public virtual Subscription Subscription { get; set; } = null!;
    public virtual SubscriptionPlan? Plan { get; set; }
}
