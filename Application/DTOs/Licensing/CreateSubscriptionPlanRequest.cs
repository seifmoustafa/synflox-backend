using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Licensing;

/// <summary>
/// Request DTO for creating a subscription plan.
/// </summary>
public class CreateSubscriptionPlanRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public required string Name { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, ErrorMessage = "Currency must be 3 characters")]
    public string Currency { get; set; } = "USD";

    [Required(ErrorMessage = "Billing cycle is required")]
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public string[]? Features { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Max companies must be positive")]
    public int? MaxCompanies { get; set; }

    public PlanTier PlanTier { get; set; } = PlanTier.Free;

    public string? ParentPlanId { get; set; }
}

