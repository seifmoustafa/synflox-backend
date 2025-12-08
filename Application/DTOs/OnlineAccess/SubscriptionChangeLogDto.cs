using System;
using Domain.Enums;

namespace Application.DTOs.OnlineAccess;

/// <summary>
/// DTO for SubscriptionChangeLog display.
/// </summary>
public class SubscriptionChangeLogDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid? PlanId { get; set; }
    public required string ChangeType { get; set; }
    public required string ChangeDescription { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public ChangeEffectPolicy EffectPolicy { get; set; }
    public DateTime EffectiveDateUtc { get; set; }
    public bool IsApplied { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
    public string? AppliedBy { get; set; }
    public bool CustomerNotified { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public int DaysUntilEffective { get; set; }
    public bool IsReadyToApply { get; set; }
    
    // Related info
    public string? SubscriptionName { get; set; }
    public string? PlanName { get; set; }
}

/// <summary>
/// DTO for pending changes summary.
/// </summary>
public class PendingChangesDto
{
    public Guid SubscriptionId { get; set; }
    public int TotalPendingChanges { get; set; }
    public DateTime? NextChangeDate { get; set; }
    public required List<SubscriptionChangeLogDto> Changes { get; set; }
}
