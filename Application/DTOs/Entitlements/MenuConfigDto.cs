namespace Application.DTOs.Entitlements;

/// <summary>
/// Menu display configuration for locked items
/// </summary>
public record MenuConfigDto
{
    /// <summary>
    /// Whether to show locked items in menu
    /// </summary>
    public bool ShowLockedItems { get; init; } = true;
    
    /// <summary>
    /// Style for locked items (greyed_with_lock, hidden, badge_only)
    /// </summary>
    public string LockedItemStyle { get; init; } = "greyed_with_lock";
    
    /// <summary>
    /// Default upgrade CTA for locked items
    /// </summary>
    public string DefaultUpgradeCta { get; init; } = "Upgrade";
    
    /// <summary>
    /// Default upgrade URL
    /// </summary>
    public string DefaultUpgradeUrl { get; init; } = "/upgrade";
}
