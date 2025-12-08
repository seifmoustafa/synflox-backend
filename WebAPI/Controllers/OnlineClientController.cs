using System;
using System.Threading.Tasks;
using Application.DTOs.Common;
using Application.DTOs.OnlineAccess;
using Application.Services;
using AutoMapper;
using Infrastructure.Resources;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace WebAPI.Controllers;

/// <summary>
/// API endpoints for online client systems.
/// These endpoints are called by client applications using thin JWT tokens.
/// 
/// Key Features:
/// - Token validation (no entitlements in token)
/// - Dynamic entitlement fetching
/// - Device registration/management
/// - Subscription status checks
/// - Pending changes visibility
/// </summary>
[Route("api/online")]
[ApiController]
public class OnlineClientController : ControllerBase
{
    private readonly IOnlineClientService _onlineClientService;
    private readonly IOnlineJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public OnlineClientController(
        IOnlineClientService onlineClientService,
        IOnlineJwtService jwtService,
        IMapper mapper,
        IStringLocalizer<SharedResource> localizer)
    {
        _onlineClientService = onlineClientService;
        _jwtService = jwtService;
        _mapper = mapper;
        _localizer = localizer;
    }

    #region Authentication

    /// <summary>
    /// Validate an online client token.
    /// </summary>
    [HttpPost("auth/validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = _localizer["OnlineToken.Required"] });
        }

        var result = await _onlineClientService.ValidateTokenAsync(request.Token);
        
        if (!result.IsValid)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }

    #endregion

    #region Entitlements

    /// <summary>
    /// Get the entitlement matrix for the authenticated client.
    /// Returns X-Entitlements-Version header for cache validation.
    /// </summary>
    [HttpGet("entitlements")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEntitlements([FromHeader(Name = "Authorization")] string? authorization)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var entitlements = await _onlineClientService.GetEntitlementsAsync(subscriptionId!.Value);
        
        // Add version header for client-side caching
        Response.Headers["X-Entitlements-Version"] = entitlements.Version.ToString();
        
        return Ok(entitlements);
    }

    /// <summary>
    /// Get just the entitlement version for cache validation.
    /// Lightweight endpoint for checking if cache is stale.
    /// </summary>
    [HttpGet("entitlements/version")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEntitlementVersion([FromHeader(Name = "Authorization")] string? authorization)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var version = await _onlineClientService.GetEntitlementVersionAsync(subscriptionId!.Value);
        
        return Ok(new { version });
    }

    #endregion

    #region Subscription Status

    /// <summary>
    /// Get subscription status for the authenticated client.
    /// </summary>
    [HttpGet("subscription/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubscriptionStatus([FromHeader(Name = "Authorization")] string? authorization)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var status = await _onlineClientService.GetSubscriptionStatusAsync(subscriptionId!.Value);
        
        return Ok(status);
    }

    /// <summary>
    /// Get pending changes for the subscription.
    /// Shows what changes will apply at next billing cycle.
    /// </summary>
    [HttpGet("subscription/pending-changes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPendingChanges([FromHeader(Name = "Authorization")] string? authorization)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var changes = await _onlineClientService.GetPendingChangesAsync(subscriptionId!.Value);
        
        return Ok(changes);
    }

    #endregion

    #region Device Management

    /// <summary>
    /// Register a device for the authenticated client.
    /// </summary>
    [HttpPost("devices/register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterDevice(
        [FromHeader(Name = "Authorization")] string? authorization,
        [FromBody] RegisterDeviceRequest request)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _onlineClientService.RegisterDeviceAsync(
            subscriptionId!.Value, 
            request, 
            ipAddress, 
            userAgent);
        
        if (!result.Success)
        {
            if (result.LimitReached)
            {
                return Conflict(result); // 409 Conflict for limit reached
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// List all registered devices for the authenticated client.
    /// </summary>
    [HttpGet("devices")]
    [AllowAnonymous]
    public async Task<IActionResult> ListDevices([FromHeader(Name = "Authorization")] string? authorization)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var devices = await _onlineClientService.GetDevicesAsync(subscriptionId!.Value);
        
        return Ok(devices);
    }

    /// <summary>
    /// Unregister a device.
    /// </summary>
    [HttpDelete("devices/{fingerprint}")]
    [AllowAnonymous]
    public async Task<IActionResult> UnregisterDevice(
        [FromHeader(Name = "Authorization")] string? authorization,
        string fingerprint)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var result = await _onlineClientService.UnregisterDeviceAsync(subscriptionId!.Value, fingerprint);
        
        if (!result)
        {
            return NotFound(new { message = _localizer["OnlineDevice.NotFound"] });
        }

        return Ok(new { message = _localizer["OnlineDevice.Unregistered"] });
    }

    /// <summary>
    /// Get device limit information.
    /// </summary>
    [HttpGet("devices/limit")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDeviceLimit([FromHeader(Name = "Authorization")] string? authorization)
    {
        var (isValid, subscriptionId, errorMessage) = await ValidateAuthorizationHeader(authorization);
        if (!isValid)
        {
            return Unauthorized(new { message = errorMessage });
        }

        var limit = await _onlineClientService.GetDeviceLimitAsync(subscriptionId!.Value);
        
        return Ok(limit);
    }

    #endregion

    #region Helpers

    private async Task<(bool IsValid, Guid? SubscriptionId, string? ErrorMessage)> ValidateAuthorizationHeader(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return (false, null, _localizer["OnlineToken.Required"]);
        }

        // Extract token from "Bearer {token}"
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization.Substring(7).Trim()
            : authorization.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, null, _localizer["OnlineToken.Required"]);
        }

        var validation = await _onlineClientService.ValidateTokenAsync(token);
        
        if (!validation.IsValid)
        {
            return (false, null, validation.Message);
        }

        // Update token usage asynchronously (fire-and-forget for performance)
        // Note: TokenId from validation is encrypted, need to decrypt via AutoMapper
        if (validation.TokenId.HasValue)
        {
            var encryptedTokenId = validation.TokenId.Value;
            var decryptedTokenId = _mapper.Map<Guid>(new TokenIdRequest { TokenId = encryptedTokenId });
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await _onlineClientService.UpdateTokenUsageAsync(decryptedTokenId, ipAddress, userAgent);
                }
                catch { /* Ignore tracking errors to not affect main flow */ }
            });
        }

        // SubscriptionId from validation is encrypted, need to decrypt via AutoMapper
        var decryptedSubscriptionId = validation.SubscriptionId.HasValue 
            ? _mapper.Map<Guid>(new SubscriptionIdRequest { SubscriptionId = validation.SubscriptionId.Value })
            : (Guid?)null;
        
        return (true, decryptedSubscriptionId, null);
    }

    #endregion
}

/// <summary>
/// Request DTO for token validation.
/// </summary>
public class ValidateTokenRequest
{
    public required string Token { get; set; }
}
