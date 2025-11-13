using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Company profile information that external clients can access
/// Contains only non-sensitive company information
/// </summary>
public class ClientCompanyProfileDto
{
    /// <summary>
    /// Basic company information
    /// </summary>
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>
    /// Contact information (if allowed by privacy settings)
    /// </summary>
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Subscription summary
    /// </summary>
    public int ActiveSubscriptions { get; set; }
    public int TotalSubscriptions { get; set; }
    public DateTime? OldestSubscriptionDate { get; set; }
    public DateTime? NewestSubscriptionDate { get; set; }

    /// <summary>
    /// Current active plans
    /// </summary>
    public List<ClientActivePlanDto> ActivePlans { get; set; } = new();

    /// <summary>
    /// Account status
    /// </summary>
    public string AccountStatus { get; set; } = string.Empty;
    public DateTime? LastActivityDate { get; set; }

    /// <summary>
    /// API usage summary
    /// </summary>
    public int TotalApiCalls { get; set; }
    public DateTime? LastApiCall { get; set; }
    public int ActiveTokens { get; set; }
}

/// <summary>
/// Active plan information for client view
/// </summary>
public class ClientActivePlanDto
{
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime ExpiryDateUtc { get; set; }
    public bool IsTrial { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Status { get; set; } = string.Empty;
}
