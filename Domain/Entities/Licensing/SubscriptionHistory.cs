using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Licensing;

/// <summary>
/// Represents a history record of subscription-related actions.
/// </summary>
public class SubscriptionHistory : BaseEntity<Guid>
{
    /// <summary>
    /// The company this history record belongs to.
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Company Company { get; set; } = null!;

    /// <summary>
    /// The type of action performed.
    /// </summary>
    [Required]
    public SubscriptionHistoryActionType ActionType { get; set; }

    /// <summary>
    /// JSON representation of the old value before the change.
    /// </summary>
    [StringLength(2000)]
    public string? OldValue { get; set; }

    /// <summary>
    /// JSON representation of the new value after the change.
    /// </summary>
    [StringLength(2000)]
    public string? NewValue { get; set; }

    /// <summary>
    /// The ID of the admin who performed the action.
    /// Null if performed by system (e.g., expiry).
    /// </summary>
    public Guid? PerformedBy { get; set; }

    /// <summary>
    /// Timestamp when the action was performed.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional notes about the action.
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}

