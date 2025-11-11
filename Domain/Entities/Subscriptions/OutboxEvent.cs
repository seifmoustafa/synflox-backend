using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Transactional outbox pattern for reliable domain event processing
/// Events are stored in database and processed asynchronously by background job
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; set; }

    /// <summary>
    /// Type of subscription event
    /// </summary>
    public SubscriptionEventType EventType { get; set; }

    /// <summary>
    /// Company that this event relates to
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Subscription that triggered this event
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// JSON payload with event data (plan names, dates, amounts, etc.)
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// When this event was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// When this event was processed (UTC)
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Whether this event has been processed
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Number of processing attempts (for retries)
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }
}
