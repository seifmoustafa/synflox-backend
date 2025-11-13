using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Plan features and capabilities that clients can access
/// </summary>
public class ClientPlanFeaturesDto
{
    /// <summary>
    /// Plan information
    /// </summary>
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanDescription { get; set; } = string.Empty;
    public bool IsTrial { get; set; }

    /// <summary>
    /// Subscription information
    /// </summary>
    public Guid SubscriptionId { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Plan features
    /// </summary>
    public List<string> Features { get; set; } = new();
    public List<ClientModuleDto> Modules { get; set; } = new();
    public Dictionary<string, object> Limits { get; set; } = new();
    public Dictionary<string, bool> Permissions { get; set; } = new();

    /// <summary>
    /// Usage allowances
    /// </summary>
    public Dictionary<string, ClientUsageAllowanceDto> UsageAllowances { get; set; } = new();

    /// <summary>
    /// API access information
    /// </summary>
    public List<string> AllowedApiEndpoints { get; set; } = new();
    public int ApiCallsPerHour { get; set; }
    public int ApiCallsPerDay { get; set; }

    /// <summary>
    /// Support information
    /// </summary>
    public string SupportLevel { get; set; } = string.Empty;
    public List<string> SupportChannels { get; set; } = new();
    public string SlaResponseTime { get; set; } = string.Empty;

    /// <summary>
    /// Upgrade information
    /// </summary>
    public bool CanUpgrade { get; set; }
    public List<ClientUpgradeOptionDto> UpgradeOptions { get; set; } = new();
}

/// <summary>
/// Module information for client view
/// </summary>
public class ClientModuleDto
{
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleDescription { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
}

/// <summary>
/// Usage allowance information
/// </summary>
public class ClientUsageAllowanceDto
{
    public string ResourceName { get; set; } = string.Empty;
    public int AllowedAmount { get; set; }
    public int UsedAmount { get; set; }
    public int RemainingAmount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime ResetDate { get; set; }
}

/// <summary>
/// Upgrade option information
/// </summary>
public class ClientUpgradeOptionDto
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AdditionalFeatures { get; set; } = new();
    public string PriceInfo { get; set; } = string.Empty;
}
