using System;
using Domain.Enums;

namespace Application.DTOs.Webhooks;

/// <summary>
/// DTO for webhook delivery records.
/// </summary>
public class WebhookDeliveryDto
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public WebhookEventType EventType { get; set; }
    public string Payload { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime AttemptedAt { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptNumber { get; set; }
}

