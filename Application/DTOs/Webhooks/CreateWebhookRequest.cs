using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Webhooks;

/// <summary>
/// Request DTO for creating a new webhook.
/// </summary>
public class CreateWebhookRequest
{
    [Required(ErrorMessage = "Company ID is required")]
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "URL cannot exceed 500 characters")]
    public required string Url { get; set; }

    [Required(ErrorMessage = "Events are required")]
    public WebhookEventType[] Events { get; set; } = Array.Empty<WebhookEventType>();

    [Range(1, 10, ErrorMessage = "Retry count must be between 1 and 10")]
    public int RetryCount { get; set; } = 3;
}

