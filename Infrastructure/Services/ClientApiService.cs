using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.ClientAccess;
using Application.DTOs.OfflineLicense;
using Application.Services;
using AutoMapper;
using Domain.Entities.Subscriptions;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for client-facing API operations
/// Provides secure, limited access to company and subscription data
/// </summary>
public class ClientApiService : IClientApiService
{
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IClientAccessTokenRepository _tokenRepo;
    private readonly IClientTokenUsageLogRepository _usageLogRepo;
    private readonly IOfflineLicenseService _offlineLicenseService;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<ClientApiService> _logger;

    public ClientApiService(
        ISubscriptionRepository subscriptionRepo,
        ICompanyRepository companyRepo,
        IClientAccessTokenRepository tokenRepo,
        IClientTokenUsageLogRepository usageLogRepo,
        IOfflineLicenseService offlineLicenseService,
        ISubscriptionPlanRepository planRepo,
        IMapper mapper,
        ILogger<ClientApiService> logger)
    {
        _subscriptionRepo = subscriptionRepo;
        _companyRepo = companyRepo;
        _tokenRepo = tokenRepo;
        _usageLogRepo = usageLogRepo;
        _offlineLicenseService = offlineLicenseService;
        _planRepo = planRepo;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets the full entitlement matrix for a subscription
    /// This is the main endpoint for thin-token architecture
    /// Clients cache this and refresh when version changes
    /// TODO: Implement with PlanEntitlement in Phase 2
    /// </summary>
    public async Task<object> GetEntitlementsAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting entitlements for subscription {SubscriptionId}", subscriptionId);
        
        // Get subscription with plan details
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new ArgumentException("Subscription not found");
            
        var plan = subscription.Plan;
        
        // TODO: Return plan.Entitlements when PlanEntitlement entity is created
        // Calculate if in grace period (expired but within grace period days)
        var isInGracePeriod = subscription.IsExpired && 
            DateTime.UtcNow <= subscription.ExpiryDateUtc.AddDays(plan.GracePeriodDays);
        
        // For now, return basic plan info
        return new
        {
            Version = plan.EntitlementVersion,
            PlanId = plan.Id,
            PlanName = plan.Name,
            AccessMode = subscription.AccessMode.ToString(),
            DaysRemaining = subscription.IsExpired ? 0 : (int)(subscription.ExpiryDateUtc - DateTime.UtcNow).TotalDays,
            IsInGracePeriod = isInGracePeriod,
            // Entitlements will come from plan.Entitlements after Phase 2
            Projects = new List<object>(),
            Modules = (plan.PlanModules ?? new List<PlanModule>()).Select(pm => new {
                ModuleId = pm.ModuleId,
                ModuleName = pm.Module?.Name ?? "Unknown",
                AccessLevel = "Full",
                CanCreate = true,
                CanRead = true,
                CanUpdate = true,
                CanDelete = true,
                CanExport = true,
                DisplayInMenu = true
            }).ToList()
        };
    }

    /// <summary>
    /// Gets subscription status for the authenticated client
    /// </summary>
    public async Task<ClientSubscriptionStatusDto> GetSubscriptionStatusAsync(Guid companyId, Guid subscriptionId)
    {
        _logger.LogInformation("Getting subscription status for company {CompanyId}, subscription {SubscriptionId}", 
            companyId, subscriptionId);

        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null || subscription.CompanyId != companyId)
            throw new ArgumentException("Subscription not found or access denied");

        var company = subscription.Company;
        var plan = subscription.Plan;

        // Calculate usage statistics (if available)
        var usageStats = new Dictionary<string, int>();
        try
        {
            // This would integrate with actual usage tracking systems
            usageStats["ApiCallsThisMonth"] = 0;
            usageStats["LicenseValidationsThisMonth"] = 0;
            usageStats["ActiveUsers"] = 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve usage statistics for subscription {SubscriptionId}", subscriptionId);
        }

        // Build plan limits dictionary
        var planLimits = new Dictionary<string, object>();
        planLimits["DurationType"] = plan.DurationType.ToString();
        planLimits["DurationDescription"] = Domain.Helpers.PlanDurationHelper.GetDurationDescription(plan.DurationType);
        planLimits["IsLifetimePlan"] = plan.IsLifetimePlan;
        planLimits["GracePeriodDays"] = plan.GracePeriodDays;
        if (plan.TrialDurationDays.HasValue) planLimits["TrialDurationDays"] = plan.TrialDurationDays.Value;

        return new ClientSubscriptionStatusDto
        {
            CompanyId = company.Id,
            CompanyName = company.Name,
            SubscriptionId = subscription.Id,
            PlanId = plan.Id,
            PlanName = plan.Name,
            PlanDescription = plan.Description ?? string.Empty,
            IsActive = subscription.IsActive,
            IsExpired = subscription.IsExpired,
            IsTrial = subscription.IsTrial,
            StartDateUtc = subscription.StartDateUtc,
            ExpiryDateUtc = subscription.ExpiryDateUtc,
            DaysUntilExpiry = subscription.IsExpired ? 0 : (int)(subscription.ExpiryDateUtc - DateTime.UtcNow).TotalDays,
            StatusReason = subscription.StatusReason ?? string.Empty,
            Features = plan.CustomFeatures ?? new List<string>(),
            Modules = plan.PlanModules?.Select(pm => pm.Module.Name).ToList() ?? new List<string>(),
            PlanLimits = planLimits,
            HasLicenseKey = !string.IsNullOrEmpty(subscription.OfflineLicenseKey),
            LicenseKeyGeneratedAt = subscription.LicenseKeyGeneratedAt,
            LicenseKeyVersion = subscription.LicenseKeyVersion,
            UsageStatistics = usageStats,
            AutoRenew = subscription.AutoRenew,
            NextSubscriptionId = subscription.NextSubscriptionId,
            NextSubscriptionPlanName = subscription.NextSubscription?.Plan?.Name,
            NextSubscriptionActivationDateUtc = subscription.NextSubscriptionActivationDateUtc
        };
    }

    /// <summary>
    /// Validates a license key for the authenticated client
    /// Uses the new enterprise-grade OfflineLicenseService
    /// </summary>
    public async Task<object> ValidateLicenseKeyAsync(string licenseKey, Guid companyId)
    {
        _logger.LogInformation("Validating license key for company {CompanyId}", companyId);
        
        var result = await _offlineLicenseService.ValidateLicenseKeyAsync(new ValidateLicenseRequest
        {
            LicenseKey = licenseKey,
            ValidateOnline = true, // Always validate online for client API
            UpdateLastValidation = true
        });
        
        // Verify the license belongs to this company
        if (result.IsValid && result.CompanyId != companyId)
        {
            _logger.LogWarning("License key company mismatch. Expected: {Expected}, Got: {Got}", 
                companyId, result.CompanyId);
            return new
            {
                IsValid = false,
                Message = "License key does not belong to this company",
                CompanyId = companyId
            };
        }
        
        return result;
    }

    /// <summary>
    /// Gets company profile information that client is allowed to see
    /// </summary>
    public async Task<ClientCompanyProfileDto> GetCompanyProfileAsync(Guid companyId)
    {
        _logger.LogInformation("Getting company profile for {CompanyId}", companyId);

        var company = await _companyRepo.GetByIdAsync(companyId, null);
        if (company == null)
            throw new ArgumentException("Company not found");

        // Get subscription summary
        var subscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        var activeSubscriptions = subscriptions.Where(s => s.IsActive && !s.IsExpired).ToList();

        // Get active plans
        var activePlans = activeSubscriptions.Select(s => new ClientActivePlanDto
        {
            SubscriptionId = s.Id,
            PlanName = s.Plan.Name,
            ExpiryDateUtc = s.ExpiryDateUtc,
            IsTrial = s.IsTrial,
            DaysUntilExpiry = s.IsExpired ? 0 : (int)(s.ExpiryDateUtc - DateTime.UtcNow).TotalDays,
            Status = s.IsActive ? "Active" : "Inactive"
        }).ToList();

        // Get API usage summary
        var activeTokens = await _tokenRepo.GetActiveTokensByCompanyIdAsync(companyId);
        var totalApiCalls = 0;
        DateTime? lastApiCall = null;

        foreach (var token in activeTokens)
        {
            var usageLogs = await _usageLogRepo.GetUsageLogsByTokenIdAsync(token.Id, 1, 1);
            var latestLog = usageLogs.FirstOrDefault();
            if (latestLog != null && (lastApiCall == null || latestLog.RequestTimestampUtc > lastApiCall))
            {
                lastApiCall = latestLog.RequestTimestampUtc;
            }
        }

        return new ClientCompanyProfileDto
        {
            CompanyId = company.Id,
            CompanyName = company.Name,
            IsActive = company.IsActive,
            CreatedTimestamp = company.CreatedTimestamp,
            ContactEmail = company.ContactEmail, // Only if privacy settings allow
            ContactPhone = company.ContactPhone, // Only if privacy settings allow
            ActiveSubscriptions = activeSubscriptions.Count,
            TotalSubscriptions = subscriptions.Count(),
            OldestSubscriptionDate = subscriptions.Any() ? subscriptions.Min(s => s.StartDateUtc) : null,
            NewestSubscriptionDate = subscriptions.Any() ? subscriptions.Max(s => s.StartDateUtc) : null,
            ActivePlans = activePlans,
            AccountStatus = company.IsActive ? "Active" : "Inactive",
            LastActivityDate = lastApiCall,
            TotalApiCalls = totalApiCalls,
            LastApiCall = lastApiCall,
            ActiveTokens = activeTokens.Count()
        };
    }

    /// <summary>
    /// Gets usage statistics for the client's token
    /// </summary>
    public async Task<ClientTokenUsageDto> GetTokenUsageStatisticsAsync(Guid tokenId, int days = 30)
    {
        _logger.LogInformation("Getting token usage statistics for {TokenId}", tokenId);

        var token = await _tokenRepo.GetByIdAsync(tokenId, null);
        if (token == null)
            throw new ArgumentException("Token not found");

        var fromDate = DateTime.UtcNow.AddDays(-days);
        var toDate = DateTime.UtcNow;

        var usageStats = await _tokenRepo.GetUsageStatisticsAsync(tokenId, fromDate, toDate);
        var endpointStats = await _usageLogRepo.GetEndpointUsageStatisticsAsync(tokenId, fromDate, toDate);
        var recentLogs = await _usageLogRepo.GetUsageLogsByTokenIdAsync(tokenId, 1, 10);
        var avgResponseTime = await _usageLogRepo.GetAverageResponseTimeAsync(tokenId, fromDate, toDate);

        return new ClientTokenUsageDto
        {
            TokenId = tokenId,
            IssuedAtUtc = token.IssuedAtUtc,
            ExpiresAtUtc = token.ExpiresAtUtc,
            LastUsedAtUtc = token.LastUsedAtUtc,
            TotalUsageCount = token.UsageCount,
            UsageCountLast24Hours = await _usageLogRepo.GetUsageCountAsync(tokenId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow),
            UsageCountLast7Days = await _usageLogRepo.GetUsageCountAsync(tokenId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            UsageCountLast30Days = await _usageLogRepo.GetUsageCountAsync(tokenId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
            AverageResponseTimeMs = avgResponseTime,
            SuccessfulRequests = usageStats.GetValueOrDefault("SuccessfulRequests", 0),
            FailedRequests = usageStats.GetValueOrDefault("FailedRequests", 0),
            SuccessRate = usageStats.GetValueOrDefault("TotalRequests", 0) > 0 
                ? (double)usageStats.GetValueOrDefault("SuccessfulRequests", 0) / usageStats.GetValueOrDefault("TotalRequests", 0) * 100 
                : 0,
            EndpointUsage = endpointStats,
            RecentActivity = _mapper.Map<List<ClientTokenUsageLogDto>>(recentLogs),
            RateLimitPerHour = 1000, // From settings
            RemainingRequestsThisHour = 950, // Would be calculated from cache
            RateLimitResetTime = DateTime.UtcNow.AddHours(1).Date.AddHours(DateTime.UtcNow.Hour + 1)
        };
    }

    /// <summary>
    /// Gets plan features and capabilities for the subscription
    /// </summary>
    public async Task<ClientPlanFeaturesDto> GetPlanFeaturesAsync(Guid subscriptionId)
    {
        _logger.LogInformation("Getting plan features for subscription {SubscriptionId}", subscriptionId);

        var subscription = await _subscriptionRepo.GetWithDetailsAsync(subscriptionId);
        if (subscription == null)
            throw new ArgumentException("Subscription not found");

        var plan = subscription.Plan;

        // Build modules list
        var modules = plan.PlanModules?.Select(pm => new ClientModuleDto
        {
            ModuleName = pm.Module.Name,
            ModuleDescription = pm.Module.Description ?? string.Empty,
            IsEnabled = true,
            Version = "1.0", // Would come from module version info
            Capabilities = new List<string>() // Would be populated from module capabilities
        }).ToList() ?? new List<ClientModuleDto>();

        // Build usage allowances based on plan features
        var usageAllowances = new Dictionary<string, ClientUsageAllowanceDto>();
        
        // Example usage allowances based on plan features
        usageAllowances["ApiCalls"] = new ClientUsageAllowanceDto
        {
            ResourceName = "API Calls",
            AllowedAmount = 10000, // Default limit
            UsedAmount = 0, // Would be calculated from actual usage
            RemainingAmount = 10000,
            Unit = "calls",
            ResetDate = subscription.ExpiryDateUtc
        };

        // Build permissions
        var permissions = new Dictionary<string, bool>
        {
            ["CanCreateProjects"] = true,
            ["CanExportData"] = !subscription.IsTrial,
            ["CanAccessAPI"] = true,
            ["CanManageUsers"] = !subscription.IsTrial
        };

        return new ClientPlanFeaturesDto
        {
            PlanId = plan.Id,
            PlanName = plan.Name,
            PlanDescription = plan.Description ?? string.Empty,
            IsTrial = subscription.IsTrial,
            SubscriptionId = subscription.Id,
            StartDateUtc = subscription.StartDateUtc,
            ExpiryDateUtc = subscription.ExpiryDateUtc,
            IsActive = subscription.IsActive,
            Features = plan.CustomFeatures ?? new List<string>(),
            Modules = modules,
            Limits = new Dictionary<string, object>
            {
                ["DurationType"] = plan.DurationType.ToString(),
                ["DurationDescription"] = Domain.Helpers.PlanDurationHelper.GetDurationDescription(plan.DurationType),
                ["IsLifetimePlan"] = plan.IsLifetimePlan,
                ["TrialDurationDays"] = plan.TrialDurationDays ?? 0,
                ["GracePeriodDays"] = plan.GracePeriodDays
            },
            Permissions = permissions,
            UsageAllowances = usageAllowances,
            AllowedApiEndpoints = new List<string>
            {
                "/api/client/subscription/status",
                "/api/client/license/validate",
                "/api/client/company/profile",
                "/api/client/health"
            },
            ApiCallsPerHour = 1000,
            ApiCallsPerDay = 10000,
            SupportLevel = subscription.IsTrial ? "Community" : "Premium",
            SupportChannels = subscription.IsTrial 
                ? new List<string> { "Documentation", "Community Forum" }
                : new List<string> { "Email", "Chat", "Phone", "Documentation", "Community Forum" },
            SlaResponseTime = subscription.IsTrial ? "Best Effort" : "24 hours",
            CanUpgrade = !subscription.IsExpired,
            UpgradeOptions = new List<ClientUpgradeOptionDto>() // Would be populated with available upgrades
        };
    }

    /// <summary>
    /// Health check endpoint for client systems
    /// </summary>
    public async Task<ClientHealthCheckDto> GetHealthCheckAsync(Guid companyId)
    {
        _logger.LogInformation("Performing health check for company {CompanyId}", companyId);

        var company = await _companyRepo.GetByIdAsync(companyId, null);
        if (company == null)
            throw new ArgumentException("Company not found");

        var subscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        var activeSubscriptions = subscriptions.Where(s => s.IsActive && !s.IsExpired).ToList();
        var activeTokens = await _tokenRepo.GetActiveTokensByCompanyIdAsync(companyId);

        // Build subscription health
        var subscriptionHealth = subscriptions.Select(s => new ClientSubscriptionHealthDto
        {
            SubscriptionId = s.Id,
            PlanName = s.Plan.Name,
            IsActive = s.IsActive,
            IsExpired = s.IsExpired,
            DaysUntilExpiry = s.IsExpired ? 0 : (int)(s.ExpiryDateUtc - DateTime.UtcNow).TotalDays,
            Status = s.IsActive && !s.IsExpired ? "Healthy" : "Warning",
            HasValidLicense = !string.IsNullOrEmpty(s.OfflineLicenseKey)
        }).ToList();

        // Build alerts
        var alerts = new List<ClientHealthAlertDto>();
        
        // Check for expiring subscriptions
        var expiringSoon = activeSubscriptions.Where(s => (s.ExpiryDateUtc - DateTime.UtcNow).TotalDays <= 7).ToList();
        foreach (var sub in expiringSoon)
        {
            alerts.Add(new ClientHealthAlertDto
            {
                Level = "Warning",
                Message = $"Subscription '{sub.Plan.Name}' expires in {(int)(sub.ExpiryDateUtc - DateTime.UtcNow).TotalDays} days",
                Component = "Subscription",
                ActionRequired = "Renew subscription before expiry"
            });
        }

        // Check for inactive company
        if (!company.IsActive)
        {
            alerts.Add(new ClientHealthAlertDto
            {
                Level = "Critical",
                Message = "Company account is inactive",
                Component = "Account",
                ActionRequired = "Contact support to reactivate account"
            });
        }

        var overallStatus = alerts.Any(a => a.Level == "Critical") ? "Critical" :
                           alerts.Any(a => a.Level == "Warning") ? "Warning" : "Healthy";

        return new ClientHealthCheckDto
        {
            Status = overallStatus,
            CheckTimestamp = DateTime.UtcNow,
            Company = new ClientCompanyHealthDto
            {
                IsActive = company.IsActive,
                Status = company.IsActive ? "Active" : "Inactive",
                ActiveSubscriptions = activeSubscriptions.Count,
                ActiveTokens = activeTokens.Count(),
                LastActivity = activeTokens.Any() ? activeTokens.Max(t => t.LastUsedAtUtc) : null
            },
            Subscriptions = subscriptionHealth,
            ApiHealth = new ClientApiHealthDto
            {
                IsAvailable = true,
                ResponseTimeMs = 150, // Would be measured
                RateLimitRemaining = 950,
                RateLimitReset = DateTime.UtcNow.AddHours(1),
                Version = "1.0"
            },
            LicenseHealth = new ClientLicenseHealthDto
            {
                IsAvailable = true,
                ValidLicenses = subscriptions.Count(s => !string.IsNullOrEmpty(s.OfflineLicenseKey)),
                ExpiredLicenses = subscriptions.Count(s => s.IsExpired),
                LastValidation = DateTime.UtcNow,
                Status = "Operational"
            },
            Alerts = alerts,
            SystemVersion = "SYNFLOX v1.0",
            SystemTime = DateTime.UtcNow,
            TimeZone = "UTC",
            Metrics = new Dictionary<string, object>
            {
                ["TotalSubscriptions"] = subscriptions.Count(),
                ["ActiveSubscriptions"] = activeSubscriptions.Count,
                ["TotalTokens"] = activeTokens.Count(),
                ["CompanyAge"] = (DateTime.UtcNow - company.CreatedTimestamp).TotalDays
            }
        };
    }

    /// <summary>
    /// Gets subscription history for the company (limited view)
    /// </summary>
    public async Task<ClientSubscriptionHistoryDto> GetSubscriptionHistoryAsync(Guid companyId, int months = 12)
    {
        _logger.LogInformation("Getting subscription history for company {CompanyId}", companyId);

        var company = await _companyRepo.GetByIdAsync(companyId, null);
        if (company == null)
            throw new ArgumentException("Company not found");

        var fromDate = DateTime.UtcNow.AddMonths(-months);
        var subscriptions = await _subscriptionRepo.GetAllByCompanyIdAsync(companyId);
        var relevantSubscriptions = subscriptions.Where(s => s.StartDateUtc >= fromDate || s.ExpiryDateUtc >= fromDate).ToList();

        // Build history entries
        var history = relevantSubscriptions.Select(s => new ClientSubscriptionHistoryEntryDto
        {
            SubscriptionId = s.Id,
            PlanName = s.Plan.Name,
            StartDate = s.StartDateUtc,
            ExpiryDate = s.ExpiryDateUtc,
            WasTrial = s.IsTrial,
            Status = s.IsActive ? "Active" : s.IsExpired ? "Expired" : "Inactive",
            EndReason = s.StatusReason,
            DurationDays = (int)(s.ExpiryDateUtc - s.StartDateUtc).TotalDays,
            AutoRenewed = s.AutoRenew,
            UpgradedToPlan = s.NextSubscription?.Plan?.Name
        }).OrderByDescending(h => h.StartDate).ToList();

        // Build summary
        var summary = new ClientSubscriptionSummaryDto
        {
            TotalSubscriptions = relevantSubscriptions.Count,
            CompletedSubscriptions = relevantSubscriptions.Count(s => s.IsExpired),
            ActiveSubscriptions = relevantSubscriptions.Count(s => s.IsActive && !s.IsExpired),
            TrialSubscriptions = relevantSubscriptions.Count(s => s.IsTrial),
            UpgradedSubscriptions = relevantSubscriptions.Count(s => s.NextSubscriptionId.HasValue),
            TotalDaysSubscribed = relevantSubscriptions.Sum(s => (int)(s.ExpiryDateUtc - s.StartDateUtc).TotalDays),
            FirstSubscriptionDate = relevantSubscriptions.Any() ? relevantSubscriptions.Min(s => s.StartDateUtc) : DateTime.MinValue,
            MostUsedPlan = relevantSubscriptions.GroupBy(s => s.Plan.Name).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "None",
            AverageSubscriptionDuration = relevantSubscriptions.Any() 
                ? relevantSubscriptions.Average(s => (s.ExpiryDateUtc - s.StartDateUtc).TotalDays) 
                : 0
        };

        // Get current active subscriptions
        var activeSubscriptions = relevantSubscriptions.Where(s => s.IsActive && !s.IsExpired)
            .Select(s => new ClientActiveSubscriptionDto
            {
                SubscriptionId = s.Id,
                PlanName = s.Plan.Name,
                StartDate = s.StartDateUtc,
                ExpiryDate = s.ExpiryDateUtc,
                DaysRemaining = (int)(s.ExpiryDateUtc - DateTime.UtcNow).TotalDays,
                IsTrial = s.IsTrial,
                AutoRenew = s.AutoRenew,
                Status = "Active",
                HasLicenseKey = !string.IsNullOrEmpty(s.OfflineLicenseKey)
            }).ToList();

        return new ClientSubscriptionHistoryDto
        {
            CompanyId = companyId,
            CompanyName = company.Name,
            FromDate = fromDate,
            ToDate = DateTime.UtcNow,
            TotalMonths = months,
            History = history,
            Summary = summary,
            ActiveSubscriptions = activeSubscriptions
        };
    }

    /// <summary>
    /// Validates token and returns basic token info
    /// </summary>
    public async Task<ClientTokenValidationDto> ValidateTokenAsync(string token)
    {
        _logger.LogInformation("Validating client token");

        try
        {
            // This would typically be handled by middleware, but providing endpoint for explicit validation
            var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
            var tokenHashString = Convert.ToHexString(tokenHash).ToLowerInvariant();
            
            var clientToken = await _tokenRepo.GetByTokenHashAsync(tokenHashString);
            if (clientToken == null)
            {
                return new ClientTokenValidationDto
                {
                    IsValid = false,
                    Status = "Invalid",
                    Message = "Token not found",
                    ErrorCode = "TOKEN_NOT_FOUND"
                };
            }

            if (!clientToken.IsValid)
            {
                return new ClientTokenValidationDto
                {
                    IsValid = false,
                    Status = clientToken.Status.ToString(),
                    Message = clientToken.IsExpired ? "Token has expired" : $"Token is {clientToken.Status}",
                    ErrorCode = clientToken.IsExpired ? "TOKEN_EXPIRED" : "TOKEN_INVALID"
                };
            }

            return new ClientTokenValidationDto
            {
                IsValid = true,
                TokenId = clientToken.Id,
                IssuedAt = clientToken.IssuedAtUtc,
                ExpiresAt = clientToken.ExpiresAtUtc,
                DaysUntilExpiry = clientToken.DaysUntilExpiry,
                CompanyId = clientToken.CompanyId,
                CompanyName = clientToken.Company.Name,
                SubscriptionId = clientToken.SubscriptionId,
                PlanName = clientToken.Subscription.Plan.Name,
                AllowedEndpoints = System.Text.Json.JsonSerializer.Deserialize<List<string>>(clientToken.AllowedEndpoints ?? "[]") ?? new(),
                Status = "Valid",
                Message = "Token is valid and active",
                LastUsed = clientToken.LastUsedAtUtc,
                UsageCount = clientToken.UsageCount,
                SubscriptionActive = clientToken.Subscription.IsActive,
                SubscriptionExpired = clientToken.Subscription.IsExpired,
                SubscriptionStatus = clientToken.Subscription.IsActive ? "Active" : "Inactive"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            
            return new ClientTokenValidationDto
            {
                IsValid = false,
                Status = "Error",
                Message = "Token validation failed",
                ErrorCode = "VALIDATION_ERROR"
            };
        }
    }
}
