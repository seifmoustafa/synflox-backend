using System;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// Request DTO for operations requiring a single encrypted Plan ID
/// Used for SYNFLOX ID encryption rule compliance
/// </summary>
public class PlanIdRequest
{
    /// <summary>
    /// Encrypted Plan ID from frontend
    /// </summary>
    public Guid PlanId { get; set; }
}
