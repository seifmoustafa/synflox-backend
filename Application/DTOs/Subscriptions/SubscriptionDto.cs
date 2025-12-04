using System;
using System.Collections.Generic;
using Application.DTOs.ModuleDto;
using Application.DTOs.ProjectDto;
using Domain.Enums;

namespace Application.DTOs.Subscriptions;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string? PlanDescription { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsTrial { get; set; }
    public bool IsExpired { get; set; }
    
    /// <summary>
    /// Indicates if this is a lifetime/permanent subscription
    /// </summary>
    public bool IsLifetime { get; set; }
    
    public bool AutoRenew { get; set; }
    
    #region Pause State (Timer Freeze)
    
    /// <summary>
    /// Whether this subscription is currently paused (timer frozen)
    /// </summary>
    public bool IsPaused { get; set; }
    
    /// <summary>
    /// When the subscription was paused
    /// </summary>
    public DateTime? PausedAtUtc { get; set; }
    
    /// <summary>
    /// Days remaining when pause was initiated
    /// </summary>
    public int? RemainingDaysWhenPaused { get; set; }
    
    #endregion
    
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public string? StatusReason { get; set; }
    /// <summary>
    /// Scheduled next subscription ID (for deferred upgrades)
    /// </summary>
    public Guid? NextSubscriptionId { get; set; }
    
    /// <summary>
    /// Next subscription plan name for display
    /// </summary>
    public string? NextSubscriptionPlanName { get; set; }
    
    /// <summary>
    /// When the next subscription should activate
    /// </summary>
    public DateTime? NextSubscriptionActivationDateUtc { get; set; }
    
    /// <summary>
    /// Parent subscription if this was created from an upgrade/renewal
    /// </summary>
    public Guid? ParentSubscriptionId { get; set; }
    
    /// <summary>
    /// Parent subscription display name for UI
    /// </summary>
    public string? ParentSubscriptionDisplayName { get; set; }
    
    // Offline License Key Management
    public string? OfflineLicenseKey { get; set; }
    public DateTime? LicenseKeyGeneratedAt { get; set; }
    public int LicenseKeyVersion { get; set; }
    public bool HasLicenseKey => !string.IsNullOrEmpty(OfflineLicenseKey);
    
    #region Access Control (Enterprise Entitlement System)
    
    /// <summary>
    /// Current access mode
    /// </summary>
    public SubscriptionAccessMode AccessMode { get; set; }
    
    /// <summary>
    /// Human-readable access mode
    /// </summary>
    public string AccessModeDisplay => AccessMode switch
    {
        SubscriptionAccessMode.Full => "Full Access",
        SubscriptionAccessMode.GracePeriod => "Grace Period",
        SubscriptionAccessMode.ReadOnly => "Read Only",
        SubscriptionAccessMode.ExportOnly => "Export Only",
        SubscriptionAccessMode.Blocked => "Blocked",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Default fallback plan ID (from plan - read only)
    /// </summary>
    public Guid? DefaultFallbackPlanId { get; set; }
    
    /// <summary>
    /// Default fallback plan name (from plan - read only)
    /// </summary>
    public string? DefaultFallbackPlanName { get; set; }
    
    /// <summary>
    /// Export deadline for ExportOnly mode
    /// </summary>
    public DateTime? ExportDeadlineUtc { get; set; }
    
    /// <summary>
    /// Entitlements version for cache invalidation
    /// </summary>
    public int EntitlementsVersion { get; set; }
    
    /// <summary>
    /// Custom restriction message
    /// </summary>
    public string? AccessRestrictionMessage { get; set; }
    
    /// <summary>
    /// Number of entitlements
    /// </summary>
    public int EntitlementCount { get; set; }
    
    /// <summary>
    /// Grace period days from plan (days after expiry before access mode changes)
    /// </summary>
    public int GracePeriodDays { get; set; }
    
    /// <summary>
    /// Export grace days from plan (days allowed for data export after blocked)
    /// </summary>
    public int ExportGraceDays { get; set; }
    
    #endregion
    
    // Computed Properties for Business Logic
    public string Status
    {
        get
        {
            if (IsLifetime) return "Lifetime";
            if (IsPaused) return "Paused";
            if (IsTrial) return "Trial";
            if (IsExpired) return "Expired";
            if (!IsActive) return "Suspended";
            
            // Check if expiring soon (within 30 days)
            var daysUntilExpiry = (ExpiryDateUtc - DateTime.UtcNow).Days;
            if (daysUntilExpiry <= 30 && daysUntilExpiry > 0) return "Expiring";
            
            return IsActive ? "Active" : "Unknown";
        }
    }
    
    public int DaysRemaining
    {
        get
        {
            if (IsLifetime) return int.MaxValue;
            var days = (ExpiryDateUtc - DateTime.UtcNow).Days;
            return Math.Max(0, days);
        }
    }
    
    // Action Permissions
    public bool CanRenew => (IsActive || IsExpired) && !IsLifetime && !IsPaused;
    public bool CanSuspend => IsActive && !IsLifetime && !IsPaused;
    public bool CanResume => !IsActive && !IsExpired && !IsLifetime && !IsPaused;
    public bool CanCancel => IsActive && !IsLifetime;
    public bool CanUpgrade => IsActive && !IsLifetime && !IsPaused;
    public bool CanExtend => IsActive && !IsLifetime && !IsPaused;
    public bool CanReactivate => IsExpired && !IsLifetime;
    public bool CanPause => IsActive && !IsLifetime && !IsPaused && !IsExpired;
    public bool CanUnpause => IsPaused;
    
    // Plan Features - Included projects, modules, and custom features
    public List<SubscriptionProjectDto> Projects { get; set; } = new();
    public List<SubscriptionModuleDto> Modules { get; set; } = new();
    public List<string> CustomFeatures { get; set; } = new();
}

/// <summary>
/// Simplified project DTO for subscription display
/// </summary>
public class SubscriptionProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<SubscriptionModuleDto> Modules { get; set; } = new();
}

/// <summary>
/// Simplified module DTO for subscription display
/// </summary>
public class SubscriptionModuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
