using System;
using Domain.Enums;

namespace Application.DTOs.Webhooks;

/// <summary>
/// DTO for webhook information.
/// </summary>
public class WebhookDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Url { get; set; } = string.Empty;
    public WebhookEventType[] Events { get; set; } = Array.Empty<WebhookEventType>();
    public bool IsActive { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}

