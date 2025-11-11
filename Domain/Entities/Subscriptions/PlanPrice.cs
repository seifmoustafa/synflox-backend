using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Multi-currency pricing for subscription plans
/// Allows one plan to have different prices in different currencies
/// </summary>
public class PlanPrice
{
    public Guid PlanId { get; set; }
    
    public Currency Currency { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 999999999)]
    public decimal Amount { get; set; }

    // Navigation
    public SubscriptionPlan Plan { get; set; } = null!;
}
