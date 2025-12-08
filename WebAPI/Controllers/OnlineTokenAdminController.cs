using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Common;
using Application.DTOs.OnlineAccess;
using Application.DTOs.Responses;
using Application.Services;
using AutoMapper;
using Infrastructure.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace WebAPI.Controllers;

/// <summary>
/// Admin endpoints for managing online client tokens.
/// Used by the admin dashboard to generate, manage, and revoke tokens.
/// </summary>
[Route("api/admin/online-tokens")]
[ApiController]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class OnlineTokenAdminController : ControllerBase
{
    private readonly IOnlineClientService _onlineClientService;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public OnlineTokenAdminController(
        IOnlineClientService onlineClientService,
        IMapper mapper,
        IStringLocalizer<SharedResource> localizer)
    {
        _onlineClientService = onlineClientService;
        _mapper = mapper;
        _localizer = localizer;
    }

    /// <summary>
    /// Generate a new online client token for a subscription.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateOnlineTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Decrypt subscription ID
        var decryptedSubscriptionId = _mapper.Map<Guid>(new SubscriptionIdRequest { SubscriptionId = request.SubscriptionId });
        request.SubscriptionId = decryptedSubscriptionId;

        var result = await _onlineClientService.GenerateTokenAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Error(result.Message, 400));
        }

        return Ok(ApiResponse<GenerateOnlineTokenResponse>.Success(result, _localizer["OnlineToken.Generated"]));
    }

    /// <summary>
    /// Get all online tokens for a company.
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<IActionResult> GetByCompany(Guid companyId, [FromQuery] bool includeRevoked = false)
    {
        // Decrypt company ID
        var decryptedCompanyId = _mapper.Map<Guid>(new CompanyIdRequest { CompanyId = companyId });
        
        var tokens = await _onlineClientService.GetTokensByCompanyAsync(decryptedCompanyId, includeRevoked);
        
        return Ok(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Success(tokens, _localizer["OnlineToken.Retrieved"]));
    }

    /// <summary>
    /// Get all online tokens for a subscription.
    /// </summary>
    [HttpGet("subscription/{subscriptionId}")]
    public async Task<IActionResult> GetBySubscription(Guid subscriptionId, [FromQuery] bool includeRevoked = false)
    {
        // Decrypt subscription ID
        var decryptedSubscriptionId = _mapper.Map<Guid>(new SubscriptionIdRequest { SubscriptionId = subscriptionId });
        
        var tokens = await _onlineClientService.GetTokensBySubscriptionAsync(decryptedSubscriptionId, includeRevoked);
        
        return Ok(ApiResponse<IEnumerable<OnlineClientTokenDto>>.Success(tokens, _localizer["OnlineToken.Retrieved"]));
    }

    /// <summary>
    /// Revoke an online client token.
    /// </summary>
    [HttpPost("{tokenId}/revoke")]
    public async Task<IActionResult> RevokeToken(Guid tokenId, [FromBody] RevokeTokenRequest request)
    {
        // Decrypt token ID
        var decryptedTokenId = _mapper.Map<Guid>(new TokenIdRequest { TokenId = tokenId });
        
        var result = await _onlineClientService.RevokeTokenAsync(decryptedTokenId, request.Reason);
        
        if (!result)
        {
            return NotFound(ApiResponse<object>.Error(_localizer["OnlineToken.NotFound"], 404));
        }

        return Ok(ApiResponse<object>.Success(null!, _localizer["OnlineToken.Revoked"]));
    }

    /// <summary>
    /// Regenerate an online client token.
    /// </summary>
    [HttpPost("{tokenId}/regenerate")]
    public async Task<IActionResult> RegenerateToken(Guid tokenId)
    {
        // Decrypt token ID
        var decryptedTokenId = _mapper.Map<Guid>(new TokenIdRequest { TokenId = tokenId });
        
        var result = await _onlineClientService.RegenerateTokenAsync(decryptedTokenId);
        
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Error(result.Message, 400));
        }

        return Ok(ApiResponse<GenerateOnlineTokenResponse>.Success(result, _localizer["OnlineToken.Generated"]));
    }

    /// <summary>
    /// Get devices for a subscription (admin view).
    /// </summary>
    [HttpGet("subscription/{subscriptionId}/devices")]
    public async Task<IActionResult> GetDevices(Guid subscriptionId)
    {
        // Decrypt subscription ID
        var decryptedSubscriptionId = _mapper.Map<Guid>(new SubscriptionIdRequest { SubscriptionId = subscriptionId });
        
        var devices = await _onlineClientService.GetDevicesAsync(decryptedSubscriptionId);
        
        return Ok(ApiResponse<IEnumerable<OnlineDeviceDto>>.Success(devices, _localizer["OnlineDevice.Retrieved"]));
    }

    /// <summary>
    /// Get device limit for a subscription.
    /// </summary>
    [HttpGet("subscription/{subscriptionId}/device-limit")]
    public async Task<IActionResult> GetDeviceLimit(Guid subscriptionId)
    {
        // Decrypt subscription ID
        var decryptedSubscriptionId = _mapper.Map<Guid>(new SubscriptionIdRequest { SubscriptionId = subscriptionId });
        
        var limit = await _onlineClientService.GetDeviceLimitAsync(decryptedSubscriptionId);
        
        return Ok(ApiResponse<DeviceLimitDto>.Success(limit, _localizer["OnlineDevice.LimitRetrieved"]));
    }

    /// <summary>
    /// Get pending changes for a subscription.
    /// </summary>
    [HttpGet("subscription/{subscriptionId}/pending-changes")]
    public async Task<IActionResult> GetPendingChanges(Guid subscriptionId)
    {
        // Decrypt subscription ID
        var decryptedSubscriptionId = _mapper.Map<Guid>(new SubscriptionIdRequest { SubscriptionId = subscriptionId });
        
        var changes = await _onlineClientService.GetPendingChangesAsync(decryptedSubscriptionId);
        
        return Ok(ApiResponse<PendingChangesDto>.Success(changes, _localizer["OnlineToken.PendingChangesRetrieved"]));
    }

    /// <summary>
    /// Unbind/unregister a device from a subscription.
    /// </summary>
    [HttpDelete("subscription/{subscriptionId}/devices/{deviceFingerprint}")]
    public async Task<IActionResult> UnbindDevice(Guid subscriptionId, string deviceFingerprint)
    {
        // Decrypt subscription ID
        var decryptedSubscriptionId = _mapper.Map<Guid>(new SubscriptionIdRequest { SubscriptionId = subscriptionId });
        
        var result = await _onlineClientService.UnregisterDeviceAsync(decryptedSubscriptionId, deviceFingerprint);
        
        if (!result)
        {
            return NotFound(ApiResponse<object>.Error(_localizer["OnlineDevice.NotFound"], 404));
        }

        return Ok(ApiResponse<object>.Success(null!, _localizer["OnlineDevice.Unbound"]));
    }

    /// <summary>
    /// Unbind a device by ID (encrypted).
    /// </summary>
    [HttpDelete("devices/{deviceId}")]
    public async Task<IActionResult> UnbindDeviceById(Guid deviceId)
    {
        // Decrypt device ID
        var decryptedDeviceId = _mapper.Map<Guid>(new DeviceIdRequest { DeviceId = deviceId });
        
        var result = await _onlineClientService.UnregisterDeviceByIdAsync(decryptedDeviceId);
        
        if (!result)
        {
            return NotFound(ApiResponse<object>.Error(_localizer["OnlineDevice.NotFound"], 404));
        }

        return Ok(ApiResponse<object>.Success(null!, _localizer["OnlineDevice.Unbound"]));
    }
}

/// <summary>
/// Request DTO for revoking a token.
/// </summary>
public class RevokeTokenRequest
{
    /// <summary>
    /// Reason for revoking the token.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(500, MinimumLength = 3)]
    public required string Reason { get; set; }
}

// DeviceIdRequest is already defined in Application.DTOs.OnlineAccess.OnlineAccessRequestDtos
