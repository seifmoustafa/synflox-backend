using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Subscriptions;

public class UpgradeSubscriptionDto
{
    [Required]
    public Guid NewPlanId { get; set; }

    /// <summary>
    /// Upgrade mode: "FullReplace", "Prorated", "Deferred", or "DefaultFromPolicy"
    /// </summary>
    [Required]
    public string Mode { get; set; } = "DefaultFromPolicy";

    /// <summary>
    /// Override AutoRenew for new subscription
    /// </summary>
    public bool? NewAutoRenew { get; set; }
}
