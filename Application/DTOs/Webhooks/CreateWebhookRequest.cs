using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs.Webhooks;

/// <summary>
/// Request DTO for creating a new webhook.
/// </summary>
public class CreateWebhookRequest
{
    [Required(ErrorMessage = "Company ID is required")]
    [JsonPropertyName("companyId")]
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "URL cannot exceed 500 characters")]
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [Required(ErrorMessage = "Events are required")]
    [JsonPropertyName("eventTypes")]
    public WebhookEventType[] Events { get; set; } = Array.Empty<WebhookEventType>();

    [Range(1, 10, ErrorMessage = "Retry count must be between 1 and 10")]
    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; } = 3;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("timeoutSeconds")]
    public int? TimeoutSeconds { get; set; }

    [JsonPropertyName("secret")]
    public string? Secret { get; set; }
}

