using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Webhooks;

/// <summary>
/// Represents a webhook configuration for a company.
/// </summary>
public class Webhook : AuditEntity<Guid>
{
    /// <summary>
    /// The company this webhook belongs to.
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Domain.Entities.Licensing.Company Company { get; set; } = null!;

    /// <summary>
    /// The URL to send webhook events to.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Url { get; set; }

    /// <summary>
    /// Secret key for HMAC signing of webhook payloads.
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string Secret { get; set; }

    /// <summary>
    /// JSON array of event types this webhook subscribes to.
    /// </summary>
    [Required]
    [StringLength(500)]
    public required string Events { get; set; } // JSON array of WebhookEventType values

    /// <summary>
    /// Whether the webhook is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Number of retry attempts for failed deliveries.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Timestamp when the webhook was last triggered.
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }
}

