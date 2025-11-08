using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;
using Domain.Enums;

namespace Domain.Entities.Webhooks;

/// <summary>
/// Represents a webhook delivery attempt.
/// </summary>
public class WebhookDelivery : BaseEntity<Guid>
{
    /// <summary>
    /// The webhook this delivery belongs to.
    /// </summary>
    [Required]
    public Guid WebhookId { get; set; }

    /// <summary>
    /// Navigation property to the webhook.
    /// </summary>
    public Webhook Webhook { get; set; } = null!;

    /// <summary>
    /// The type of event that triggered this delivery.
    /// </summary>
    [Required]
    public WebhookEventType EventType { get; set; }

    /// <summary>
    /// JSON payload that was sent.
    /// </summary>
    [Required]
    [StringLength(5000)]
    public required string Payload { get; set; }

    /// <summary>
    /// HTTP status code received from the webhook endpoint.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Response body received from the webhook endpoint.
    /// </summary>
    [StringLength(2000)]
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Timestamp when the delivery was attempted.
    /// </summary>
    [Required]
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the delivery was successful.
    /// </summary>
    [Required]
    public bool Succeeded { get; set; }

    /// <summary>
    /// Error message if delivery failed.
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Attempt number (1, 2, 3, etc.).
    /// </summary>
    public int AttemptNumber { get; set; } = 1;
}

