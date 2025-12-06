using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.ClientAdmin;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for client administrators to manage their device bindings.
/// Uses Client Admin Token for authentication (JWT in Authorization header).
/// </summary>
[ApiController]
[Route("api/client/devices")]
[AllowAnonymous] // Uses custom token authentication, not standard JWT
public class ClientDeviceController : ControllerBase
{
    private readonly IOfflineLicenseAdminService _adminService;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<ClientDeviceController> _logger;

    public ClientDeviceController(
        IOfflineLicenseAdminService adminService,
        ILocalizationService localizer,
        ILogger<ClientDeviceController> logger)
    {
        _adminService = adminService;
        _localizer = localizer;
        _logger = logger;
    }

    #region Device Binding

    /// <summary>
    /// Bind a device to a subscription.
    /// </summary>
    /// <param name="request">Binding request with subscription ID and fingerprint</param>
    /// <returns>Binding result</returns>
    [HttpPost("bind")]
    public async Task<ActionResult<ApiResponse<DeviceBindingResponse>>> BindDevice(
        [FromBody] BindDeviceRequest request)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<DeviceBindingResponse>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var result = await _adminService.BindDeviceAsync(context, request);
            
            if (result.Success)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<DeviceBindingResponse>.Success(result, result.Message));
            }
            
            return BadRequest(ApiResponse<DeviceBindingResponse>.Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error binding device for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<DeviceBindingResponse>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Bind multiple devices at once.
    /// </summary>
    /// <param name="request">Bulk binding request</param>
    /// <returns>Bulk binding results</returns>
    [HttpPost("bind-bulk")]
    public async Task<ActionResult<ApiResponse<BulkDeviceBindingResponse>>> BindDevicesBulk(
        [FromBody] BulkBindDeviceRequest request)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<BulkDeviceBindingResponse>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var result = await _adminService.BindDevicesBulkAsync(context, request);
            await RecordUsageAsync(context);
            
            var message = $"{result.SuccessCount} devices bound successfully, {result.FailedCount} failed";
            return Ok(ApiResponse<BulkDeviceBindingResponse>.Success(result, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk binding devices for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<BulkDeviceBindingResponse>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Unbind a device from a subscription.
    /// </summary>
    /// <param name="request">Unbind request</param>
    /// <returns>Success status</returns>
    [HttpPost("unbind")]
    public async Task<ActionResult<ApiResponse<bool>>> UnbindDevice(
        [FromBody] UnbindDeviceRequest request)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<bool>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var result = await _adminService.UnbindDeviceAsync(context, request);
            
            if (result)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<bool>.Success(true, _localizer["OfflineLicense.DeviceDeactivated"]));
            }
            
            return NotFound(ApiResponse<bool>.Error(_localizer["OfflineLicense.ActivationNotFound"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unbinding device for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }

    #endregion

    #region Device Viewing

    /// <summary>
    /// Get all bound devices for a subscription.
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <returns>List of bound devices</returns>
    [HttpGet("subscription/{subscriptionId}")]
    public async Task<ActionResult<ApiResponse<List<BoundDeviceDto>>>> GetBoundDevices(
        Guid subscriptionId)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<List<BoundDeviceDto>>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var devices = await _adminService.GetBoundDevicesAsync(context, subscriptionId);
            await RecordUsageAsync(context);
            return Ok(ApiResponse<List<BoundDeviceDto>>.Success(devices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bound devices for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<List<BoundDeviceDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Get device summary for all company subscriptions.
    /// </summary>
    /// <returns>Company device summary</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<CompanyDeviceSummary>>> GetCompanySummary()
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<CompanyDeviceSummary>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var summary = await _adminService.GetCompanyDeviceSummaryAsync(context);
            await RecordUsageAsync(context);
            return Ok(ApiResponse<CompanyDeviceSummary>.Success(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company summary for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<CompanyDeviceSummary>.Error(ex.Message));
        }
    }

    #endregion

    #region Replacement Requests

    /// <summary>
    /// Get pending device replacement requests.
    /// </summary>
    /// <returns>List of pending requests</returns>
    [HttpGet("replacements/pending")]
    public async Task<ActionResult<ApiResponse<List<DeviceReplacementRequestDto>>>> GetPendingReplacements()
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<List<DeviceReplacementRequestDto>>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var requests = await _adminService.GetPendingReplacementRequestsAsync(context);
            await RecordUsageAsync(context);
            return Ok(ApiResponse<List<DeviceReplacementRequestDto>>.Success(requests));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting replacement requests for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<List<DeviceReplacementRequestDto>>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Approve a device replacement request.
    /// </summary>
    /// <param name="requestId">Replacement request ID</param>
    /// <returns>Binding result</returns>
    [HttpPost("replacements/{requestId}/approve")]
    public async Task<ActionResult<ApiResponse<DeviceBindingResponse>>> ApproveReplacement(Guid requestId)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<DeviceBindingResponse>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var result = await _adminService.ApproveReplacementRequestAsync(context, requestId);
            
            if (result.Success)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<DeviceBindingResponse>.Success(result, result.Message));
            }
            
            return BadRequest(ApiResponse<DeviceBindingResponse>.Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving replacement {RequestId}", requestId);
            return BadRequest(ApiResponse<DeviceBindingResponse>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Reject a device replacement request.
    /// </summary>
    /// <param name="requestId">Replacement request ID</param>
    /// <param name="reason">Rejection reason</param>
    /// <returns>Success status</returns>
    [HttpPost("replacements/{requestId}/reject")]
    public async Task<ActionResult<ApiResponse<bool>>> RejectReplacement(
        Guid requestId,
        [FromQuery] string? reason = null)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<bool>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            var result = await _adminService.RejectReplacementRequestAsync(context, requestId, reason ?? "Rejected by admin");
            
            if (result)
            {
                await RecordUsageAsync(context);
                return Ok(ApiResponse<bool>.Success(true, "Replacement request rejected"));
            }
            
            return NotFound(ApiResponse<bool>.Error("Replacement request not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting replacement {RequestId}", requestId);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Create a device replacement request.
    /// Used when max devices reached and user wants to add a new device.
    /// </summary>
    /// <param name="request">Replacement request with new device fingerprint</param>
    /// <returns>Replacement request result</returns>
    [HttpPost("replacements/request")]
    public async Task<ActionResult<ApiResponse<CreateReplacementResponse>>> CreateReplacementRequest(
        [FromBody] CreateReplacementRequest request)
    {
        var context = await ValidateTokenAsync();
        if (context == null)
            return Unauthorized(ApiResponse<CreateReplacementResponse>.Error(_localizer["OfflineLicense.InvalidToken"]));

        try
        {
            // Add request metadata
            request.RequestedFromIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.RequestedUserAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var result = await _adminService.CreateReplacementRequestAsync(context, request);
            await RecordUsageAsync(context);
            
            if (result.Success)
            {
                return Ok(ApiResponse<CreateReplacementResponse>.Success(result, result.Message));
            }
            
            return BadRequest(ApiResponse<CreateReplacementResponse>.Error(result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating replacement request for company {CompanyId}", context.CompanyId);
            return BadRequest(ApiResponse<CreateReplacementResponse>.Error(ex.Message));
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
        // This is just a placeholder for additional tracking if needed
        await Task.CompletedTask;
    }

    #endregion
}
