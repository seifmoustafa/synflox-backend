using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.ClientAccess;
// Application.DTOs.Entitlements REMOVED - v2.0: Will be replaced with PlanEntitlement DTOs
using Application.DTOs.Responses;
using Application.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers;

/// <summary>
/// Public API controller for external client systems
/// Provides secure endpoints for companies to query their subscription status and validate licenses
/// </summary>
[ApiController]
[Route("api/client")]
[Produces("application/json")]
public class ClientApiController : ControllerBase
{
    private readonly IClientApiService _clientApiService;
    private readonly IClientTokenService _clientTokenService;
    private readonly ClientJwtService _jwtService;
    private readonly ILogger<ClientApiController> _logger;

    public ClientApiController(
        IClientApiService clientApiService,
        IClientTokenService clientTokenService,
        ClientJwtService jwtService,
        ILogger<ClientApiController> logger)
    {
        _clientApiService = clientApiService;
        _clientTokenService = clientTokenService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a client access token
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>Token validation result</returns>
    [HttpPost("auth/validate-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientTokenValidationDto>>> ValidateToken([FromBody] string token)
    {
        try
        {
            _logger.LogInformation("Token validation requested");

            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(ApiResponse<ClientTokenValidationDto>.Error("Token is required"));
            }

            var result = await _clientApiService.ValidateTokenAsync(token);
            
            if (result.IsValid)
            {
                // Record the validation request
                if (result.TokenId.HasValue)
                {
                    await _clientTokenService.RecordTokenUsageAsync(
                        result.TokenId.Value,
                        "/api/client/auth/validate-token",
                        "POST",
                        200,
                        0, // Response time would be measured by middleware
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        HttpContext.Request.Headers.UserAgent.ToString()
                    );
                }

                return Ok(ApiResponse<ClientTokenValidationDto>.Success(result, "Token is valid"));
            }

            return Unauthorized(ApiResponse<ClientTokenValidationDto>.Error(result.Message ?? "Token validation failed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, ApiResponse<ClientTokenValidationDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets the full entitlement matrix for the authenticated subscription
    /// This is the main endpoint for the thin-token architecture
    /// Clients should cache this response and refresh when X-Entitlements-Version header changes
    /// </summary>
    /// <returns>Complete entitlement matrix</returns>
    [HttpGet("entitlements")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetEntitlements(CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionId = _jwtService.GetSubscriptionId(HttpContext.User);
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!subscriptionId.HasValue)
            {
                return BadRequest(ApiResponse<object>.Error("Invalid token claims"));
            }

            _logger.LogInformation("Getting entitlements for subscription {SubscriptionId}", subscriptionId);

            var result = await _clientApiService.GetEntitlementsAsync(subscriptionId.Value, cancellationToken);

            // Add entitlements version header for cache validation
            // Get version from result using reflection (since it's dynamic object)
            var versionProperty = result.GetType().GetProperty("Version");
            var version = versionProperty?.GetValue(result)?.ToString() ?? "1";
            Response.Headers.Append("X-Entitlements-Version", version);
            Response.Headers.Append("Cache-Control", "private, max-age=86400"); // 24 hours

            // Record API usage
            if (tokenId.HasValue)
            {
                await _clientTokenService.RecordTokenUsageAsync(
                    tokenId.Value,
                    "/api/client/entitlements",
                    "GET",
                    200,
                    0,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString()
                );
            }

            return Ok(ApiResponse<object>.Success(result, "Entitlements retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for entitlements");
            return BadRequest(ApiResponse<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entitlements");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets subscription status for the authenticated client
    /// </summary>
    /// <returns>Subscription status information</returns>
    [HttpGet("subscription/status")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientSubscriptionStatusDto>>> GetSubscriptionStatus()
    {
        try
        {
            var companyId = _jwtService.GetCompanyId(HttpContext.User);
            var subscriptionId = _jwtService.GetSubscriptionId(HttpContext.User);
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!companyId.HasValue || !subscriptionId.HasValue)
            {
                return BadRequest(ApiResponse<ClientSubscriptionStatusDto>.Error("Invalid token claims"));
            }

            _logger.LogInformation("Getting subscription status for company {CompanyId}, subscription {SubscriptionId}", 
                companyId, subscriptionId);

            var result = await _clientApiService.GetSubscriptionStatusAsync(companyId.Value, subscriptionId.Value);

            // Record API usage
            if (tokenId.HasValue)
            {
                await _clientTokenService.RecordTokenUsageAsync(
                    tokenId.Value,
                    "/api/client/subscription/status",
                    "GET",
                    200,
                    0,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString()
                );
            }

            return Ok(ApiResponse<ClientSubscriptionStatusDto>.Success(result, "Subscription status retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for subscription status");
            return BadRequest(ApiResponse<ClientSubscriptionStatusDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription status");
            return StatusCode(500, ApiResponse<ClientSubscriptionStatusDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Validates a license key for the authenticated client
    /// Uses enterprise-grade AES-256-GCM encrypted license validation
    /// </summary>
    /// <param name="licenseKey">The license key to validate</param>
    /// <returns>License validation result with entitlements</returns>
    [HttpPost("license/validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ValidateLicenseKey([FromBody] string licenseKey)
    {
        try
        {
            var companyId = _jwtService.GetCompanyId(HttpContext.User);
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!companyId.HasValue)
            {
                return BadRequest(ApiResponse<object>.Error("Invalid token claims"));
            }

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                return BadRequest(ApiResponse<object>.Error("License key is required"));
            }

            _logger.LogInformation("Validating license key for company {CompanyId}", companyId);

            var result = await _clientApiService.ValidateLicenseKeyAsync(licenseKey, companyId.Value);

            // Determine if validation was successful
            var isValid = result is Application.DTOs.OfflineLicense.ValidateLicenseResponse response && response.IsValid;

            // Record API usage
            if (tokenId.HasValue)
            {
                await _clientTokenService.RecordTokenUsageAsync(
                    tokenId.Value,
                    "/api/client/license/validate",
                    "POST",
                    isValid ? 200 : 400,
                    0,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString(),
                    isValid ? null : "License validation failed"
                );
            }

            return Ok(ApiResponse<object>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license key");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets company profile information for the authenticated client
    /// </summary>
    /// <returns>Company profile information</returns>
    [HttpGet("company/profile")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientCompanyProfileDto>>> GetCompanyProfile()
    {
        try
        {
            var companyId = _jwtService.GetCompanyId(HttpContext.User);
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!companyId.HasValue)
            {
                return BadRequest(ApiResponse<ClientCompanyProfileDto>.Error("Invalid token claims"));
            }

            _logger.LogInformation("Getting company profile for {CompanyId}", companyId);

            var result = await _clientApiService.GetCompanyProfileAsync(companyId.Value);

            // Record API usage
            if (tokenId.HasValue)
            {
                await _clientTokenService.RecordTokenUsageAsync(
                    tokenId.Value,
                    "/api/client/company/profile",
                    "GET",
                    200,
                    0,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString()
                );
            }

            return Ok(ApiResponse<ClientCompanyProfileDto>.Success(result, "Company profile retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for company profile");
            return BadRequest(ApiResponse<ClientCompanyProfileDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company profile");
            return StatusCode(500, ApiResponse<ClientCompanyProfileDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets usage statistics for the authenticated client's token
    /// </summary>
    /// <param name="days">Number of days to include in statistics (default: 30)</param>
    /// <returns>Token usage statistics</returns>
    [HttpGet("usage/statistics")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientTokenUsageDto>>> GetUsageStatistics([FromQuery] int days = 30)
    {
        try
        {
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!tokenId.HasValue)
            {
                return BadRequest(ApiResponse<ClientTokenUsageDto>.Error("Invalid token claims"));
            }

            if (days < 1 || days > 365)
            {
                return BadRequest(ApiResponse<ClientTokenUsageDto>.Error("Days must be between 1 and 365"));
            }

            _logger.LogInformation("Getting usage statistics for token {TokenId}", tokenId);

            var result = await _clientApiService.GetTokenUsageStatisticsAsync(tokenId.Value, days);

            // Record API usage
            await _clientTokenService.RecordTokenUsageAsync(
                tokenId.Value,
                "/api/client/usage/statistics",
                "GET",
                200,
                0,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()
            );

            return Ok(ApiResponse<ClientTokenUsageDto>.Success(result, "Usage statistics retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for usage statistics");
            return BadRequest(ApiResponse<ClientTokenUsageDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics");
            return StatusCode(500, ApiResponse<ClientTokenUsageDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets plan features and capabilities for the authenticated client's subscription
    /// </summary>
    /// <returns>Plan features information</returns>
    [HttpGet("plan/features")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientPlanFeaturesDto>>> GetPlanFeatures()
    {
        try
        {
            var subscriptionId = _jwtService.GetSubscriptionId(HttpContext.User);
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!subscriptionId.HasValue)
            {
                return BadRequest(ApiResponse<ClientPlanFeaturesDto>.Error("Invalid token claims"));
            }

            _logger.LogInformation("Getting plan features for subscription {SubscriptionId}", subscriptionId);

            var result = await _clientApiService.GetPlanFeaturesAsync(subscriptionId.Value);

            // Record API usage
            if (tokenId.HasValue)
            {
                await _clientTokenService.RecordTokenUsageAsync(
                    tokenId.Value,
                    "/api/client/plan/features",
                    "GET",
                    200,
                    0,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString()
                );
            }

            return Ok(ApiResponse<ClientPlanFeaturesDto>.Success(result, "Plan features retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for plan features");
            return BadRequest(ApiResponse<ClientPlanFeaturesDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting plan features");
            return StatusCode(500, ApiResponse<ClientPlanFeaturesDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Health check endpoint for client systems
    /// </summary>
    /// <returns>System health information</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientHealthCheckDto>>> GetHealthCheck()
    {
        try
        {
            var companyId = _jwtService.GetCompanyId(HttpContext.User);
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!companyId.HasValue)
            {
                return BadRequest(ApiResponse<ClientHealthCheckDto>.Error("Invalid token claims"));
            }

            _logger.LogInformation("Performing health check for company {CompanyId}", companyId);

            var result = await _clientApiService.GetHealthCheckAsync(companyId.Value);

            // Record API usage
            if (tokenId.HasValue)
            {
                await _clientTokenService.RecordTokenUsageAsync(
                    tokenId.Value,
                    "/api/client/health",
                    "GET",
                    200,
                    0,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString()
                );
            }

            return Ok(ApiResponse<ClientHealthCheckDto>.Success(result, "Health check completed successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for health check");
            return BadRequest(ApiResponse<ClientHealthCheckDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check");
            return StatusCode(500, ApiResponse<ClientHealthCheckDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets subscription history for the authenticated client's company
    /// </summary>
    /// <param name="months">Number of months to include in history (default: 12)</param>
    /// <returns>Subscription history information</returns>
    [HttpGet("subscription/history")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ClientSubscriptionHistoryDto>>> GetSubscriptionHistory([FromQuery] int months = 12)
    {
        try
        {
            var companyId = _jwtService.GetCompanyId(HttpContext.User);
            var tokenId = _jwtService.GetClientTokenId(HttpContext.User);

            if (!companyId.HasValue)
            {
                return BadRequest(ApiResponse<ClientSubscriptionHistoryDto>.Error("Invalid token claims"));
            }

            if (months < 1 || months > 60)
            {
                return BadRequest(ApiResponse<ClientSubscriptionHistoryDto>.Error("Months must be between 1 and 60"));
            }

            _logger.LogInformation("Getting subscription history for company {CompanyId}", companyId);

            var result = await _clientApiService.GetSubscriptionHistoryAsync(companyId.Value, months);

            // Record API usage
            if (tokenId.HasValue)
            {
                await _clientTokenService.RecordTokenUsageAsync(
                    tokenId.Value,
                    "/api/client/subscription/history",
                    "GET",
                    200,
                    0,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers.UserAgent.ToString()
                );
            }

            return Ok(ApiResponse<ClientSubscriptionHistoryDto>.Success(result, "Subscription history retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for subscription history");
            return BadRequest(ApiResponse<ClientSubscriptionHistoryDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription history");
            return StatusCode(500, ApiResponse<ClientSubscriptionHistoryDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets API documentation and usage examples
    /// </summary>
    /// <returns>API documentation</returns>
    [HttpGet("docs")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> GetApiDocumentation()
    {
        var documentation = new
        {
            Version = "1.0",
            Description = "SYNFLOX Client API - Secure endpoints for external client systems",
            Authentication = new
            {
                Type = "Bearer Token (JWT)",
                Header = "Authorization: Bearer {your-token}",
                Note = "Contact your administrator to obtain a client access token"
            },
            Endpoints = new[]
            {
                new { Method = "POST", Path = "/api/client/auth/validate-token", Description = "Validate a client token", Auth = "None" },
                new { Method = "GET", Path = "/api/client/entitlements", Description = "Get full entitlement matrix (MAIN ENDPOINT)", Auth = "Required" },
                new { Method = "GET", Path = "/api/client/subscription/status", Description = "Get subscription status", Auth = "Required" },
                new { Method = "POST", Path = "/api/client/license/validate", Description = "Validate license key", Auth = "Required" },
                new { Method = "GET", Path = "/api/client/company/profile", Description = "Get company profile", Auth = "Required" },
                new { Method = "GET", Path = "/api/client/usage/statistics", Description = "Get usage statistics", Auth = "Required" },
                new { Method = "GET", Path = "/api/client/plan/features", Description = "Get plan features", Auth = "Required" },
                new { Method = "GET", Path = "/api/client/health", Description = "Health check", Auth = "Required" },
                new { Method = "GET", Path = "/api/client/subscription/history", Description = "Get subscription history", Auth = "Required" }
            },
            RateLimits = new
            {
                PerHour = 1000,
                PerDay = 10000,
                PerMonth = 100000
            },
            Support = new
            {
                Documentation = "https://docs.synflox.com/client-api",
                Contact = "support@synflox.com"
            }
        };

        return Ok(ApiResponse<object>.Success(documentation, "API documentation retrieved successfully"));
    }
}
