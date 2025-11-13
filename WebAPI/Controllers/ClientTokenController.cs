using System;
using System.Threading.Tasks;
using Application.DTOs.ClientAccess;
using Application.DTOs.Responses;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers;

/// <summary>
/// Admin controller for managing client access tokens
/// Provides endpoints for admins to generate, revoke, and monitor client tokens
/// </summary>
[ApiController]
[Route("api/admin/client-tokens")]
[Authorize(Policy = "SuperAdminOnly")]
[Produces("application/json")]
public class ClientTokenController : ControllerBase
{
    private readonly IClientTokenService _clientTokenService;
    private readonly IMapper _mapper;
    private readonly ILogger<ClientTokenController> _logger;

    public ClientTokenController(
        IClientTokenService clientTokenService,
        IMapper mapper,
        ILogger<ClientTokenController> logger)
    {
        _clientTokenService = clientTokenService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new client access token for a subscription
    /// </summary>
    /// <param name="request">Token generation request</param>
    /// <returns>Generated token response</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<GenerateClientTokenResponse>>> GenerateToken([FromBody] GenerateClientTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<GenerateClientTokenResponse>.Error("Invalid request data"));
            }

            _logger.LogInformation("Admin generating client token for subscription {SubscriptionId}", request.SubscriptionId);

            var result = await _clientTokenService.GenerateTokenAsync(request);
            return Ok(ApiResponse<GenerateClientTokenResponse>.Success(result, "Client token generated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for token generation");
            return BadRequest(ApiResponse<GenerateClientTokenResponse>.Error(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot generate token");
            return BadRequest(ApiResponse<GenerateClientTokenResponse>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating client token");
            return StatusCode(500, ApiResponse<GenerateClientTokenResponse>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets all tokens for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <returns>List of company tokens</returns>
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetCompanyTokens(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Admin getting tokens for company {CompanyId}", companyId);

            var tokens = await _clientTokenService.GetCompanyTokensAsync(companyId);
            return Ok(ApiResponse<object>.Success(tokens, "Company tokens retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company tokens");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets all tokens for a subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <returns>List of subscription tokens</returns>
    [HttpGet("subscription/{subscriptionId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetSubscriptionTokens(Guid subscriptionId)
    {
        try
        {
            // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
            var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = subscriptionId };
            var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);
            _logger.LogInformation("Admin getting tokens for subscription {SubscriptionId}", decryptedSubscriptionId);

            var tokens = await _clientTokenService.GetSubscriptionTokensAsync(decryptedSubscriptionId);
            return Ok(ApiResponse<object>.Success(tokens, "Subscription tokens retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription tokens");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Revokes a specific token
    /// </summary>
    /// <param name="tokenId">Token ID to revoke</param>
    /// <param name="reason">Reason for revocation</param>
    /// <returns>Revocation result</returns>
    [HttpDelete("{tokenId}/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeToken(Guid tokenId, [FromBody] string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(ApiResponse<object>.Error("Revocation reason is required"));
            }

            _logger.LogInformation("Admin revoking token {TokenId}: {Reason}", tokenId, reason);

            var success = await _clientTokenService.RevokeTokenAsync(tokenId, reason);
            
            if (success)
            {
                return Ok(ApiResponse<object>.Success(null, "Token revoked successfully"));
            }

            return NotFound(ApiResponse<object>.Error("Token not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Revokes all tokens for a subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="reason">Reason for revocation</param>
    /// <returns>Number of tokens revoked</returns>
    [HttpDelete("subscription/{subscriptionId}/revoke-all")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeAllSubscriptionTokens(Guid subscriptionId, [FromBody] string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(ApiResponse<object>.Error("Revocation reason is required"));
            }

            // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
            var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = subscriptionId };
            var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);
            _logger.LogInformation("Admin revoking all tokens for subscription {SubscriptionId}: {Reason}", decryptedSubscriptionId, reason);

            var count = await _clientTokenService.RevokeAllSubscriptionTokensAsync(decryptedSubscriptionId, reason);
            return Ok(ApiResponse<object>.Success(new { RevokedCount = count }, $"Revoked {count} tokens successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking subscription tokens");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Regenerates token for a subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="reason">Reason for regeneration</param>
    /// <returns>New token response</returns>
    [HttpPost("subscription/{subscriptionId}/regenerate")]
    public async Task<ActionResult<ApiResponse<GenerateClientTokenResponse>>> RegenerateToken(Guid subscriptionId, [FromBody] string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(ApiResponse<GenerateClientTokenResponse>.Error("Regeneration reason is required"));
            }

            // Decrypt subscription ID from route parameter (SYNFLOX ID encryption rule compliance)
            var subscriptionIdRequest = new SubscriptionIdRequest { SubscriptionId = subscriptionId };
            var decryptedSubscriptionId = _mapper.Map<Guid>(subscriptionIdRequest);
            _logger.LogInformation("Admin regenerating token for subscription {SubscriptionId}: {Reason}", decryptedSubscriptionId, reason);

            var result = await _clientTokenService.RegenerateTokenAsync(decryptedSubscriptionId, reason);
            return Ok(ApiResponse<GenerateClientTokenResponse>.Success(result, "Token regenerated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for token regeneration");
            return BadRequest(ApiResponse<GenerateClientTokenResponse>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating token");
            return StatusCode(500, ApiResponse<GenerateClientTokenResponse>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets token usage statistics
    /// </summary>
    /// <param name="tokenId">Token ID</param>
    /// <param name="days">Number of days for statistics</param>
    /// <returns>Usage statistics</returns>
    [HttpGet("{tokenId}/usage")]
    public async Task<ActionResult<ApiResponse<ClientTokenUsageDto>>> GetTokenUsage(Guid tokenId, [FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
            {
                return BadRequest(ApiResponse<ClientTokenUsageDto>.Error("Days must be between 1 and 365"));
            }

            _logger.LogInformation("Admin getting usage for token {TokenId}", tokenId);

            var result = await _clientTokenService.GetTokenUsageAsync(tokenId, days);
            return Ok(ApiResponse<ClientTokenUsageDto>.Success(result, "Token usage retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for token usage");
            return BadRequest(ApiResponse<ClientTokenUsageDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token usage");
            return StatusCode(500, ApiResponse<ClientTokenUsageDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets tokens expiring within specified days
    /// </summary>
    /// <param name="days">Days from now to check for expiring tokens</param>
    /// <returns>List of expiring tokens</returns>
    [HttpGet("expiring")]
    public async Task<ActionResult<ApiResponse<object>>> GetExpiringTokens([FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 365)
            {
                return BadRequest(ApiResponse<object>.Error("Days must be between 1 and 365"));
            }

            _logger.LogInformation("Admin getting tokens expiring in {Days} days", days);

            var tokens = await _clientTokenService.GetExpiringTokensAsync(days);
            return Ok(ApiResponse<object>.Success(tokens, $"Found {tokens.Count()} tokens expiring in {days} days"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring tokens");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Gets comprehensive token analytics
    /// </summary>
    /// <param name="fromDate">Start date for analytics</param>
    /// <param name="toDate">End date for analytics</param>
    /// <returns>Token analytics data</returns>
    [HttpGet("analytics")]
    public async Task<ActionResult<ApiResponse<object>>> GetTokenAnalytics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            if (from >= to)
            {
                return BadRequest(ApiResponse<object>.Error("From date must be before to date"));
            }

            if ((to - from).TotalDays > 365)
            {
                return BadRequest(ApiResponse<object>.Error("Date range cannot exceed 365 days"));
            }

            _logger.LogInformation("Admin getting token analytics from {FromDate} to {ToDate}", from, to);

            var analytics = await _clientTokenService.GetTokenAnalyticsAsync(from, to);
            return Ok(ApiResponse<object>.Success(analytics, "Token analytics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token analytics");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Cleans up expired tokens
    /// </summary>
    /// <param name="olderThanDays">Remove tokens expired longer than this many days</param>
    /// <returns>Number of tokens cleaned up</returns>
    [HttpDelete("cleanup")]
    public async Task<ActionResult<ApiResponse<object>>> CleanupExpiredTokens([FromQuery] int olderThanDays = 30)
    {
        try
        {
            if (olderThanDays < 1 || olderThanDays > 365)
            {
                return BadRequest(ApiResponse<object>.Error("Days must be between 1 and 365"));
            }

            _logger.LogInformation("Admin cleaning up tokens expired longer than {Days} days", olderThanDays);

            var count = await _clientTokenService.CleanupExpiredTokensAsync(olderThanDays);
            return Ok(ApiResponse<object>.Success(new { CleanedCount = count }, $"Cleaned up {count} expired tokens"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired tokens");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }
}
