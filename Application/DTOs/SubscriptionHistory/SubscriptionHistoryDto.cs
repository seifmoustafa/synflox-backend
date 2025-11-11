using System;
using Domain.Enums;

namespace Application.DTOs.SubscriptionHistory;

/// <summary>
/// DTO for subscription history records.
/// </summary>
public class SubscriptionHistoryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public SubscriptionHistoryActionType ActionType { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
    public string ActionTypeName { get; set; } = string.Empty;
}

