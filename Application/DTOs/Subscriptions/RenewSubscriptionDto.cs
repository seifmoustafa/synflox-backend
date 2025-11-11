using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Subscriptions;

public class RenewSubscriptionDto
{
    /// <summary>
    /// How to handle the renewal
    /// "CreateFollowUp" (preferred) or "ExtendInPlace"
    /// </summary>
    [Required]
    public string RenewStrategy { get; set; } = "CreateFollowUp";

    /// <summary>
    /// Override AutoRenew setting for new subscription
    /// </summary>
    public bool? NewAutoRenew { get; set; }

    /// <summary>
    /// Optionally schedule a next plan
    /// </summary>
    public Guid? NextPlanId { get; set; }

    public DateTime? NextPlanStartDateUtc { get; set; }
}
