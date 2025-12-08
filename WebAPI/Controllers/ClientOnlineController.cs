using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.ClientAdmin;
using Application.DTOs.OnlineAccess;
using Application.DTOs.Responses;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for client administrators to manage their online tokens and devices.
/// Uses Client Admin Token for authentication (JWT in Authorization header).
/// This is the UNIFIED admin system - same token manages both offline and online devices.
/// </summary>
[ApiController]
[Route("api/client/online")]
[AllowAnonymous] // Uses custom token authentication, not standard JWT
public class ClientOnlineController : ControllerBase
{
    private readonly IOfflineLicenseAdminService _adminService;
    private readonly IOnlineClientService _onlineService;
    private readonly ILocalizationService _localizer;
    private readonly IMapper _mapper;
    private readonly ILogger<ClientOnlineController> _logger;

    public ClientOnlineController(
        IOfflineLicenseAdminService adminService,
        IOnlineClientService onlineService,
        ILocalizationService localizer,
        IMapper mapper,
        ILogger<ClientOnlineController> logger)
    {
        _adminService = adminService;
        _onlineService = onlineService;
        _localizer = localizer;
        _mapper = mapper;
        _logger = logger;
    }

    #region Online Token Management

    /// <summary>
    /// Get all online tokens for the company.
    /// Requires: CanViewOnlineTokens permission
    /// </summary>
    [HttpGet("tokens")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OnlineClientTokenDto>>>> GetTokens(
        [FromQuery] bool includeRevoked = false)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanViewOnlineTokens)
            return Forbid();

        try
        {
            var tokens = await _onlineService.GetTokensByCompanyAsync(context.CompanyId, includeRevoked);
            await RecordUsageAsync(context);
            return Ok(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Success(tokens, _localizer["OnlineToken.Retrieved"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online tokens for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Get online tokens for a specific subscription.
    /// Requires: CanViewOnlineTokens permission
    /// </summary>
    [HttpGet("tokens/subscription/{subscriptionId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OnlineClientTokenDto>>>> GetTokensBySubscription(
        Guid subscriptionId,
        [FromQuery] bool includeRevoked = false)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanViewOnlineTokens)
            return Forbid();

        try
        {
            // Verify subscription belongs to company (done inside service)
            var tokens = await _onlineService.GetTokensBySubscriptionAsync(subscriptionId, includeRevoked);
            await RecordUsageAsync(context);
            return Ok(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Success(tokens, _localizer["OnlineToken.Retrieved"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online tokens for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Generate a new online token for a subscription.
    /// Requires: CanManageOnlineTokens permission
    /// </summary>
    [HttpPost("tokens/generate")]
    public async Task<ActionResult<ApiResponse<GenerateOnlineTokenResponse>>> GenerateToken(
        [FromBody] GenerateOnlineTokenRequest request)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<GenerateOnlineTokenResponse>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanManageOnlineTokens)
            return Forbid();

        try
        {
            var result = await _onlineService.GenerateTokenAsync(request);
            
            if (result.Success)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<GenerateOnlineTokenResponse>.Success(result, _localizer["OnlineToken.Generated"]));
            }
            
            return BadRequest(ApiResponse<GenerateOnlineTokenResponse>.Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating online token for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<GenerateOnlineTokenResponse>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Revoke an online token.
    /// Requires: CanManageOnlineTokens permission
    /// </summary>
    [HttpPost("tokens/{tokenId}/revoke")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeToken(
        Guid tokenId,
        [FromBody] ClientRevokeTokenRequest request)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<bool>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanManageOnlineTokens)
            return Forbid();

        try
        {
            var result = await _onlineService.RevokeTokenAsync(tokenId, request.Reason);
            
            if (result)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<bool>.Success(true, _localizer["OnlineToken.Revoked"]));
            }
            
            return NotFound(ApiResponse<bool>.Error(_localizer["OnlineToken.NotFound"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking online token {TokenId}", tokenId);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Regenerate an online token.
    /// Requires: CanManageOnlineTokens permission
    /// </summary>
    [HttpPost("tokens/{tokenId}/regenerate")]
    public async Task<ActionResult<ApiResponse<GenerateOnlineTokenResponse>>> RegenerateToken(Guid tokenId)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<GenerateOnlineTokenResponse>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanManageOnlineTokens)
            return Forbid();

        try
        {
            var result = await _onlineService.RegenerateTokenAsync(tokenId);
            
            if (result.Success)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<GenerateOnlineTokenResponse>.Success(result, _localizer["OnlineToken.Generated"]));
            }
            
            return BadRequest(ApiResponse<GenerateOnlineTokenResponse>.Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating online token {TokenId}", tokenId);
            return BadRequest(ApiResponse<GenerateOnlineTokenResponse>.Error(ex.Message));
        }
    }

    #endregion

    #region Online Device Management

    /// <summary>
    /// Get all online devices for a subscription.
    /// Requires: CanViewOnlineDevices permission
    /// </summary>
    [HttpGet("devices/subscription/{subscriptionId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OnlineDeviceDto>>>> GetDevices(Guid subscriptionId)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<IEnumerable<OnlineDeviceDto>>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanViewOnlineDevices)
            return Forbid();

        try
        {
            var devices = await _onlineService.GetDevicesAsync(subscriptionId);
            await RecordUsageAsync(context);
            return Ok(ApiResponse<IEnumerable<OnlineDeviceDto>>.Success(devices, _localizer["OnlineDevice.Retrieved"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online devices for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<IEnumerable<OnlineDeviceDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Get device limit for a subscription.
    /// Requires: CanViewOnlineDevices permission
    /// </summary>
    [HttpGet("devices/subscription/{subscriptionId}/limit")]
    public async Task<ActionResult<ApiResponse<DeviceLimitDto>>> GetDeviceLimit(Guid subscriptionId)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<DeviceLimitDto>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanViewOnlineDevices)
            return Forbid();

        try
        {
            var limit = await _onlineService.GetDeviceLimitAsync(subscriptionId);
            await RecordUsageAsync(context);
            return Ok(ApiResponse<DeviceLimitDto>.Success(limit, _localizer["OnlineDevice.LimitRetrieved"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device limit for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<DeviceLimitDto>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Unbind an online device.
    /// Requires: CanUnbindOnlineDevices permission
    /// </summary>
    [HttpDelete("devices/{deviceId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UnbindDevice(Guid deviceId)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<bool>.Error(_localizer["OfflineLicense.InvalidToken"]));

        if (!context.CanUnbindOnlineDevices)
            return Forbid();

        try
        {
            var result = await _onlineService.UnregisterDeviceByIdAsync(deviceId);
            
            if (result)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<bool>.Success(true, _localizer["OnlineDevice.Unbound"]));
            }
            
            return NotFound(ApiResponse<bool>.Error(_localizer["OnlineDevice.NotFound"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbinding online device {DeviceId}", deviceId);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }

    #endregion

    #region Private Helpers

    private async Task<ClientAdminContext?> ValidateTokenAsync()
    {
        var authHeader = HttpContext.Request.Headers.Authorization.ToString();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        return await _adminService.ValidateAdminTokenAsync(token);
    }

    private async Task RecordUsageAsync(ClientAdminContext context)
    {
        // Usage is recorded in ValidateAdminTokenAsync
        await Task.CompletedTask;
    }

    #endregion
}

/// <summary>
/// Request DTO for client to revoke a token.
/// </summary>
public class ClientRevokeTokenRequest
{
    /// <summary>
    /// Reason for revoking the token.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(500, MinimumLength = 3)]
    public required string Reason { get; set; }
}
