using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Domain.Entities.Common;

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
    /// Gets all subscriptions with pagination and search
    /// </summary>
    Task<(IEnumerable<SubscriptionDto> Items, PaginationMetadata Pagination)> GetAllSubscriptionsAsync(int page, int pageSize, string? search = null);

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
    /// Cancels a subscription immediately with reason and email notification
    /// Sets IsActive=false and IsExpired=true
    /// </summary>
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string reason, string? language = null);

    /// <summary>
    /// Suspends a subscription temporarily
    /// </summary>
    Task<bool> SuspendSubscriptionAsync(Guid subscriptionId, string reason, string? language = null);

    /// <summary>
    /// Resumes a suspended subscription
    /// </summary>
    Task<bool> ResumeSubscriptionAsync(Guid subscriptionId, string reason, string? language = null);

    /// <summary>
    /// Pauses a subscription temporarily (preserves trial time)
    /// </summary>
    Task<bool> PauseSubscriptionAsync(Guid subscriptionId, string reason, string? language = null);

    /// <summary>
    /// Unpauses a paused subscription
    /// </summary>
    Task<bool> UnpauseSubscriptionAsync(Guid subscriptionId, string reason, string? language = null);

    /// <summary>
    /// Stops trial and converts to paid subscription immediately
    /// </summary>
    Task<bool> StopTrialAsync(Guid subscriptionId, string reason, string? language = null);

    /// <summary>
    /// Extends subscription expiry date by specified days
    /// </summary>
    Task<SubscriptionDto> ExtendSubscriptionAsync(Guid subscriptionId, ExtendSubscriptionDto dto, string? language = null);

    /// <summary>
    /// Reactivates an expired subscription
    /// </summary>
    Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId, string reason, string? language = null);

    /// <summary>
    /// Gets subscription history and audit trail
    /// </summary>
    Task<IEnumerable<object>> GetSubscriptionHistoryAsync(Guid subscriptionId);

    /// <summary>
    /// Gets subscription analytics and usage statistics
    /// </summary>
    Task<SubscriptionAnalyticsDto> GetSubscriptionAnalyticsAsync(Guid subscriptionId, DateTime? fromDate, DateTime? toDate);
}
