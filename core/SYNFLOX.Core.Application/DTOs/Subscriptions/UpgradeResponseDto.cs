using System;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// Detailed response for subscription upgrades with commercial summary
/// </summary>
public class UpgradeResponseDto
{
    /// <summary>
    /// Localized success message for user feedback
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    public string Mode { get; set; } = string.Empty;
    public string OldPlanName { get; set; } = string.Empty;
    public string NewPlanName { get; set; } = string.Empty;
    
    public SubscriptionWindowDto OldWindow { get; set; } = new();
    public SubscriptionWindowDto NewWindow { get; set; } = new();
    
    public ProrationSuggestionDto? ProrationSuggestion { get; set; }
    
    public SubscriptionDto? NewSubscription { get; set; }
}

public class SubscriptionWindowDto
{
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public int DurationDays { get; set; }
}

public class ProrationSuggestionDto
{
    public int RemainingDays { get; set; }
    public decimal OldDailyRate { get; set; }
    public decimal SuggestedCredit { get; set; }
    public decimal NewDailyRate { get; set; }
    public decimal SuggestedCharge { get; set; }
    public decimal NetDue { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
}
