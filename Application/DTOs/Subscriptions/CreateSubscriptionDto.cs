using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class CreateSubscriptionDto
{
    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public Currency Currency { get; set; }

    /// <summary>
    /// Whether to start with trial (if plan allows it)
    /// </summary>
    public bool StartWithTrial { get; set; }

    /// <summary>
    /// Override default AutoRenew from plan
    /// </summary>
    public bool? AutoRenew { get; set; }

    /// <summary>
    /// Optionally schedule a next plan (deferred upgrade)
    /// </summary>
    public Guid? NextPlanId { get; set; }

    public DateTime? NextPlanStartDateUtc { get; set; }
}
