using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;

namespace Application.Services;

/// <summary>
/// Service for managing company subscriptions and lifecycle operations
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Creates a new subscription for a company
    /// </summary>
    Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto dto);

    /// <summary>
    /// Gets a subscription by ID with full details
    /// </summary>
    Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid id);

    /// <summary>
    /// Gets the active subscription for a company
    /// Returns null if no active subscription exists
    /// </summary>
    Task<SubscriptionDto?> GetActiveSubscriptionAsync(Guid companyId);

    /// <summary>
    /// Gets all subscriptions for a company (active and historical)
    /// </summary>
    Task<IEnumerable<SubscriptionDto>> GetCompanySubscriptionsAsync(Guid companyId);

    /// <summary>
    /// Gets detailed status information for a subscription
    /// Includes computed status messages (localized)
    /// </summary>
    Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(Guid subscriptionId);

    /// <summary>
    /// Renews a subscription (creates follow-up or extends in place)
    /// </summary>
    Task<SubscriptionDto> RenewSubscriptionAsync(Guid subscriptionId, RenewSubscriptionDto dto);

    /// <summary>
    /// Upgrades a subscription to a new plan
    /// Supports FullReplace, Prorated, and Deferred modes
    /// Returns detailed upgrade response with commercial summary
    /// </summary>
    Task<UpgradeResponseDto> UpgradeSubscriptionAsync(Guid subscriptionId, UpgradeSubscriptionDto dto);

    /// <summary>
    /// Cancels a subscription immediately
    /// Sets IsActive=false and IsExpired=true
    /// </summary>
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId);
}
