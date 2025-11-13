using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.ClientAccess;
using Application.Services;
using AutoMapper;
using Domain.Entities.ClientAccess;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Enterprise-grade client token service with comprehensive lifecycle management
/// </summary>
public class ClientTokenService : IClientTokenService
{
    private readonly IClientAccessTokenRepository _tokenRepo;
    private readonly IClientTokenUsageLogRepository _usageLogRepo;
    private readonly ISubscriptionRepository _subscriptionRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ClientJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly ILogger<ClientTokenService> _logger;
    private readonly ClientTokenSettings _settings;

    public ClientTokenService(
        IClientAccessTokenRepository tokenRepo,
        IClientTokenUsageLogRepository usageLogRepo,
        ISubscriptionRepository subscriptionRepo,
        ICompanyRepository companyRepo,
        IUnitOfWork unitOfWork,
        ClientJwtService jwtService,
        IMapper mapper,
        ILogger<ClientTokenService> logger,
        IOptions<ClientTokenSettings> settings)
    {
        _tokenRepo = tokenRepo;
        _usageLogRepo = usageLogRepo;
        _subscriptionRepo = subscriptionRepo;
        _companyRepo = companyRepo;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _mapper = mapper;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// Generates a new client access token for a subscription
    /// </summary>
    public async Task<GenerateClientTokenResponse> GenerateTokenAsync(GenerateClientTokenRequest request)
    {
        _logger.LogInformation("BEFORE DECRYPTION: Original subscription ID from request: {OriginalId}", request.SubscriptionId);
        
        // Decrypt subscription ID using AutoMapper (SYNFLOX ID encryption rule compliance)
        var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = request.SubscriptionId };
        var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);
        
        _logger.LogInformation("AFTER DECRYPTION: Decrypted subscription ID: {DecryptedId}", decryptedSubscriptionId);
        _logger.LogInformation("Generating client token for subscription {SubscriptionId}", decryptedSubscriptionId);

        // Get subscription with full details
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(decryptedSubscriptionId);
        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found. Original ID: {OriginalId}, Decrypted ID: {DecryptedId}", 
                request.SubscriptionId, decryptedSubscriptionId);
            throw new ArgumentException($"Subscription {request.SubscriptionId} not found (decrypted: {decryptedSubscriptionId})");
        }

        if (!subscription.IsActive)
            throw new InvalidOperationException("Cannot generate token for inactive subscription");

        // Revoke existing tokens if requested
        if (request.RevokeExistingTokens)
        {
            await RevokeAllSubscriptionTokensAsync(decryptedSubscriptionId, "New token generated");
        }

        // Determine token expiry
        var expiryDate = request.CustomExpiryDate ?? 
                        (_settings.UseSubscriptionExpiry ? subscription.ExpiryDateUtc : DateTime.UtcNow.AddDays(_settings.DefaultExpiryDays));

        // Ensure expiry doesn't exceed maximum allowed
        var maxExpiry = DateTime.UtcNow.AddDays(_settings.MaxExpiryDays);
        if (expiryDate > maxExpiry)
            expiryDate = maxExpiry;

        // Create client token entity
        var clientToken = new ClientAccessToken
        {
            Id = Guid.NewGuid(),
            CompanyId = subscription.CompanyId,
            SubscriptionId = decryptedSubscriptionId,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiryDate,
            Status = ClientTokenStatus.Active,
            AllowedEndpoints = request.AllowedEndpoints?.Any() == true 
                ? JsonSerializer.Serialize(MapEndpointTypesToPaths(request.AllowedEndpoints))
                : JsonSerializer.Serialize(_settings.DefaultAllowedEndpoints),
            TokenVersion = _settings.TokenVersion,
            TokenHash = string.Empty // Will be set after JWT generation
        };

        // Generate JWT token
        var jwtToken = _jwtService.GenerateToken(clientToken, subscription);
        clientToken.TokenHash = _jwtService.GenerateTokenHash(jwtToken);

        // Save to database
        await _tokenRepo.AddAsync(clientToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Client token {TokenId} generated successfully for subscription {SubscriptionId}", 
            clientToken.Id, request.SubscriptionId);

        // Return response
        return new GenerateClientTokenResponse
        {
            AccessToken = jwtToken,
            TokenId = clientToken.Id,
            IssuedAtUtc = clientToken.IssuedAtUtc,
            ExpiresAtUtc = clientToken.ExpiresAtUtc,
            AllowedEndpoints = JsonSerializer.Deserialize<List<string>>(clientToken.AllowedEndpoints) ?? new(),
            TokenVersion = clientToken.TokenVersion,
            CompanyId = subscription.CompanyId,
            CompanyName = subscription.Company.Name,
            SubscriptionId = subscription.Id,
            PlanName = subscription.Plan.Name
        };
    }

    /// <summary>
    /// Generates a client token directly with decrypted subscription ID (internal use)
    /// </summary>
    private async Task<GenerateClientTokenResponse> GenerateTokenDirectlyAsync(Guid decryptedSubscriptionId, string? reason = null, bool revokeExisting = false)
    {
        _logger.LogInformation("Generating client token directly for subscription {SubscriptionId}", decryptedSubscriptionId);

        // Get subscription with full details
        var subscription = await _subscriptionRepo.GetWithDetailsAsync(decryptedSubscriptionId);
        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found. Decrypted ID: {DecryptedId}", decryptedSubscriptionId);
            throw new ArgumentException($"Subscription {decryptedSubscriptionId} not found");
        }

        if (!subscription.IsActive)
            throw new InvalidOperationException("Cannot generate token for inactive subscription");

        // Revoke existing tokens if requested
        if (revokeExisting)
        {
            await RevokeAllSubscriptionTokensAsync(decryptedSubscriptionId, "New token generated");
        }

        // Determine token expiry
        var expiryDate = _settings.UseSubscriptionExpiry ? subscription.ExpiryDateUtc : DateTime.UtcNow.AddDays(_settings.DefaultExpiryDays);

        // Ensure expiry doesn't exceed maximum allowed
        var maxExpiry = DateTime.UtcNow.AddDays(_settings.MaxExpiryDays);
        if (expiryDate > maxExpiry)
            expiryDate = maxExpiry;

        // Create client token entity
        var clientToken = new ClientAccessToken
        {
            Id = Guid.NewGuid(),
            CompanyId = subscription.CompanyId,
            SubscriptionId = decryptedSubscriptionId,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiryDate,
            Status = ClientTokenStatus.Active,
            AllowedEndpoints = JsonSerializer.Serialize(_settings.DefaultAllowedEndpoints),
            TokenVersion = _settings.TokenVersion,
            TokenHash = string.Empty // Will be set after JWT generation
        };

        // Generate JWT token
        var jwtToken = _jwtService.GenerateToken(clientToken, subscription);
        clientToken.TokenHash = _jwtService.GenerateTokenHash(jwtToken);

        // Save to database
        await _tokenRepo.AddAsync(clientToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Client token {TokenId} generated successfully for subscription {SubscriptionId}", 
            clientToken.Id, clientToken.SubscriptionId);

        // Map to response DTO
        var response = _mapper.Map<GenerateClientTokenResponse>(clientToken);
        response.AccessToken = jwtToken; // AccessToken is only set here, not stored in DB
        response.CompanyName = subscription.Company.Name;
        response.PlanName = subscription.Plan.Name;

        return response;
    }

    /// <summary>
    /// Maps ClientEndpointType enum values to actual endpoint paths
    /// </summary>
    private List<string> MapEndpointTypesToPaths(IEnumerable<ClientEndpointType> endpointTypes)
    {
        var endpointMap = new Dictionary<ClientEndpointType, string>
        {
            { ClientEndpointType.SubscriptionStatus, "/api/client/subscription/status" },
            { ClientEndpointType.LicenseValidation, "/api/client/license/validate" },
            { ClientEndpointType.UsageStatistics, "/api/client/usage/statistics" },
            { ClientEndpointType.CompanyProfile, "/api/client/company/profile" },
            { ClientEndpointType.TokenValidation, "/api/client/auth/validate-token" },
            { ClientEndpointType.PlanFeatures, "/api/client/plan/features" },
            { ClientEndpointType.HealthCheck, "/api/client/health" },
            { ClientEndpointType.SubscriptionHistory, "/api/client/subscription/history" },
            { ClientEndpointType.Documentation, "/api/client/docs" }
        };

        return endpointTypes
            .Where(et => endpointMap.ContainsKey(et))
            .Select(et => endpointMap[et])
            .ToList();
    }

    /// <summary>
    /// Validates a client token and returns token information
    /// </summary>
    public async Task<ClientTokenDto?> ValidateTokenAsync(string token)
    {
        try
        {
            // Validate JWT signature and claims
            var (isValid, principal, error) = _jwtService.ValidateToken(token);
            if (!isValid || principal == null)
            {
                _logger.LogWarning("Token validation failed: {Error}", error);
                return null;
            }

            // Get token from database
            var tokenHash = _jwtService.GenerateTokenHash(token);
            var clientToken = await _tokenRepo.GetByTokenHashAsync(tokenHash);
            
            if (clientToken == null)
            {
                _logger.LogWarning("Token not found in database: {TokenHash}", tokenHash);
                return null;
            }

            // Check token status and expiry
            if (!clientToken.IsValid)
            {
                _logger.LogWarning("Token {TokenId} is not valid. Status: {Status}, Expired: {IsExpired}", 
                    clientToken.Id, clientToken.Status, clientToken.IsExpired);
                return null;
            }

            // Update usage statistics
            await _tokenRepo.UpdateTokenUsageAsync(clientToken.Id, null, null);

            return _mapper.Map<ClientTokenDto>(clientToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }

    /// <summary>
    /// Gets all tokens for a company
    /// </summary>
    public async Task<IEnumerable<ClientTokenDto>> GetCompanyTokensAsync(Guid companyId)
    {
        var tokens = await _tokenRepo.GetActiveTokensByCompanyIdAsync(companyId);
        return _mapper.Map<IEnumerable<ClientTokenDto>>(tokens);
    }

    /// <summary>
    /// Gets all tokens for a subscription
    /// </summary>
    public async Task<IEnumerable<ClientTokenDto>> GetSubscriptionTokensAsync(Guid subscriptionId)
    {
        var tokens = await _tokenRepo.GetTokensBySubscriptionIdAsync(subscriptionId);
        return _mapper.Map<IEnumerable<ClientTokenDto>>(tokens);
    }

    /// <summary>
    /// Revokes a specific token
    /// </summary>
    public async Task<bool> RevokeTokenAsync(Guid tokenId, string reason)
    {
        var token = await _tokenRepo.GetByIdAsync(tokenId, null);
        if (token == null)
            return false;

        token.Status = ClientTokenStatus.Revoked;
        token.RevocationReason = reason;
        token.UpdatedTimestamp = DateTime.UtcNow;

        await _tokenRepo.UpdateAsync(token);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Token {TokenId} revoked: {Reason}", tokenId, reason);
        return true;
    }

    /// <summary>
    /// Revokes all tokens for a subscription
    /// </summary>
    public async Task<int> RevokeAllSubscriptionTokensAsync(Guid subscriptionId, string reason)
    {
        await _tokenRepo.RevokeAllTokensForSubscriptionAsync(subscriptionId, reason);
        
        var revokedCount = (await _tokenRepo.GetTokensBySubscriptionIdAsync(subscriptionId))
            .Count(t => t.Status == ClientTokenStatus.Revoked);

        _logger.LogInformation("Revoked {Count} tokens for subscription {SubscriptionId}: {Reason}", 
            revokedCount, subscriptionId, reason);

        return revokedCount;
    }

    /// <summary>
    /// Revokes all tokens for a company
    /// </summary>
    public async Task<int> RevokeAllCompanyTokensAsync(Guid companyId, string reason)
    {
        await _tokenRepo.RevokeAllTokensForCompanyAsync(companyId, reason);
        
        var revokedCount = (await _tokenRepo.GetActiveTokensByCompanyIdAsync(companyId)).Count();

        _logger.LogInformation("Revoked {Count} tokens for company {CompanyId}: {Reason}", 
            revokedCount, companyId, reason);

        return revokedCount;
    }

    /// <summary>
    /// Regenerates token for a subscription (revokes old, creates new)
    /// </summary>
    public async Task<GenerateClientTokenResponse> RegenerateTokenAsync(Guid subscriptionId, string reason)
    {
        _logger.LogInformation("Regenerating token for subscription {SubscriptionId}: {Reason}", subscriptionId, reason);

        // Revoke existing tokens
        await RevokeAllSubscriptionTokensAsync(subscriptionId, $"Token regenerated: {reason}");

        // Generate new token directly with decrypted ID (avoid double decryption)
        return await GenerateTokenDirectlyAsync(subscriptionId, reason, false);
    }

    /// <summary>
    /// Gets comprehensive token usage statistics
    /// </summary>
    public async Task<ClientTokenUsageDto> GetTokenUsageAsync(Guid tokenId, int days = 30)
    {
        var token = await _tokenRepo.GetByIdAsync(tokenId, null);
        if (token == null)
            throw new ArgumentException($"Token {tokenId} not found");

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
            RateLimitPerHour = _settings.RateLimitPerHour,
            RemainingRequestsThisHour = CalculateRemainingRequests(tokenId),
            RateLimitResetTime = DateTime.UtcNow.AddHours(1).Date.AddHours(DateTime.UtcNow.Hour + 1)
        };
    }

    /// <summary>
    /// Records token usage for analytics and monitoring
    /// </summary>
    public async Task RecordTokenUsageAsync(Guid tokenId, string endpoint, string httpMethod, 
        int statusCode, long responseTimeMs, string? clientIp, string? userAgent, string? errorMessage = null)
    {
        var usageLog = new ClientTokenUsageLog
        {
            Id = Guid.NewGuid(),
            ClientTokenId = tokenId,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            ClientIpAddress = clientIp,
            UserAgent = userAgent,
            ResponseStatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            RequestTimestampUtc = DateTime.UtcNow,
            ErrorMessage = errorMessage
        };

        await _usageLogRepo.AddAsync(usageLog);
        await _tokenRepo.UpdateTokenUsageAsync(tokenId, clientIp, userAgent);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if a token has access to a specific endpoint
    /// </summary>
    public async Task<bool> HasEndpointAccessAsync(string token, string endpoint)
    {
        var (isValid, principal, _) = _jwtService.ValidateToken(token);
        if (!isValid || principal == null)
            return false;

        return _jwtService.HasEndpointAccess(principal, endpoint);
    }

    /// <summary>
    /// Gets tokens expiring within specified days
    /// </summary>
    public async Task<IEnumerable<ClientTokenDto>> GetExpiringTokensAsync(int daysFromNow)
    {
        var tokens = await _tokenRepo.GetExpiringTokensAsync(daysFromNow);
        return _mapper.Map<IEnumerable<ClientTokenDto>>(tokens);
    }

    /// <summary>
    /// Cleans up expired tokens
    /// </summary>
    public async Task<int> CleanupExpiredTokensAsync(int olderThanDays = 30)
    {
        var expiredTokens = await _tokenRepo.GetTokensByStatusAsync(ClientTokenStatus.Expired);
        var countBefore = expiredTokens.Count();

        await _tokenRepo.CleanupExpiredTokensAsync(olderThanDays);
        await _usageLogRepo.CleanupOldLogsAsync(_settings.CleanupUsageLogsAfterDays);

        _logger.LogInformation("Cleaned up {Count} expired tokens older than {Days} days", countBefore, olderThanDays);
        return countBefore;
    }

    /// <summary>
    /// Updates token status based on subscription changes
    /// </summary>
    public async Task UpdateTokensForSubscriptionStatusAsync(Guid subscriptionId, bool isActive, string? reason = null)
    {
        if (!isActive && _settings.RevokeOnSubscriptionInactive)
        {
            await RevokeAllSubscriptionTokensAsync(subscriptionId, reason ?? "Subscription became inactive");
        }
    }

    /// <summary>
    /// Gets comprehensive token analytics for admin dashboard
    /// </summary>
    public async Task<Dictionary<string, object>> GetTokenAnalyticsAsync(DateTime fromDate, DateTime toDate)
    {
        var allTokens = await _tokenRepo.GetAllAsync();
        var usageLogs = await _usageLogRepo.GetUsageLogsByDateRangeAsync(fromDate, toDate);
        var topEndpoints = await _usageLogRepo.GetTopEndpointsAsync(fromDate, toDate);

        return new Dictionary<string, object>
        {
            ["TotalTokens"] = allTokens.Count(),
            ["ActiveTokens"] = allTokens.Count(t => t.Status == ClientTokenStatus.Active),
            ["ExpiredTokens"] = allTokens.Count(t => t.Status == ClientTokenStatus.Expired),
            ["RevokedTokens"] = allTokens.Count(t => t.Status == ClientTokenStatus.Revoked),
            ["TotalApiCalls"] = usageLogs.Count(),
            ["SuccessfulCalls"] = usageLogs.Count(log => log.IsSuccessful),
            ["FailedCalls"] = usageLogs.Count(log => !log.IsSuccessful),
            ["AverageResponseTime"] = usageLogs.Any() ? usageLogs.Average(log => log.ResponseTimeMs) : 0,
            ["TopEndpoints"] = topEndpoints,
            ["UniqueCompanies"] = allTokens.Select(t => t.CompanyId).Distinct().Count(),
            ["TokensExpiringIn7Days"] = (await GetExpiringTokensAsync(7)).Count()
        };
    }

    /// <summary>
    /// Automatically generates token when subscription is created
    /// </summary>
    public async Task<GenerateClientTokenResponse> AutoGenerateTokenForSubscriptionAsync(Guid subscriptionId)
    {
        if (!_settings.AutoGenerateForNewSubscriptions)
            throw new InvalidOperationException("Auto-generation is disabled");

        var request = new GenerateClientTokenRequest
        {
            SubscriptionId = subscriptionId,
            Reason = "Auto-generated for new subscription"
        };

        return await GenerateTokenAsync(request);
    }


    /// <summary>
    /// Calculates remaining API requests for rate limiting
    /// </summary>
    private int CalculateRemainingRequests(Guid tokenId)
    {
        // This would typically use a cache like Redis for real-time rate limiting
        // For now, return a placeholder calculation
        var currentHour = DateTime.UtcNow.Hour;
        var usedThisHour = 0; // Would be retrieved from cache
        return Math.Max(0, _settings.RateLimitPerHour - usedThisHour);
    }
}
