using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for managing offline license keys tied to subscriptions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all license operations
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _licenseService;
    private readonly ILocalizationService _localizer;

    public LicenseController(
        ILicenseService licenseService,
        ILocalizationService localizer)
    {
        _licenseService = licenseService;
        _localizer = localizer;
    }

    /// <summary>
    /// Generate offline license key for a subscription
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <returns>Generated license key</returns>
    [HttpPost("generate/{subscriptionId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GenerateLicenseKey(Guid subscriptionId)
    {
        try
        {
            var result = await _licenseService.GenerateOfflineLicenseKeyAsync(subscriptionId);
            return Ok(new ApiResponse<GenerateLicenseKeyResponse>(200, string.Empty, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Regenerate offline license key for a subscription
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <returns>New license key</returns>
    [HttpPost("regenerate/{subscriptionId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> RegenerateLicenseKey(Guid subscriptionId)
    {
        try
        {
            var result = await _licenseService.RegenerateOfflineLicenseKeyAsync(subscriptionId);
            return Ok(new ApiResponse<GenerateLicenseKeyResponse>(200, string.Empty, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Validate offline license key (used by client applications)
    /// </summary>
    /// <param name="request">License key validation request</param>
    /// <returns>Validation result with subscription details</returns>
    [HttpPost("validate")]
    [AllowAnonymous] // Allow anonymous access for client validation
    public async Task<IActionResult> ValidateLicenseKey([FromBody] ValidateLicenseKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _licenseService.ValidateOfflineLicenseKeyAsync(request);
            return Ok(new ApiResponse<LicenseKeyValidationResponse>(200, string.Empty, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Get all license keys for a company
    /// </summary>
    /// <param name="companyId">Encrypted company ID</param>
    /// <returns>List of license keys with subscription details</returns>
    [HttpGet("company/{companyId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetCompanyLicenseKeys(Guid companyId)
    {
        try
        {
            var result = await _licenseService.GetCompanyLicenseKeysAsync(companyId);
            return Ok(new ApiResponse<IEnumerable<CompanyLicenseKeyDto>>(200, string.Empty, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Revoke/invalidate a license key
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("revoke/{subscriptionId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> RevokeLicenseKey(Guid subscriptionId)
    {
        try
        {
            var result = await _licenseService.RevokeLicenseKeyAsync(subscriptionId);
            return Ok(new ApiResponse<bool>(200, _localizer["License.KeyRevokedSuccessfully"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Check if a subscription has a valid license key
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <returns>True if has valid key</returns>
    [HttpGet("check/{subscriptionId}")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<IActionResult> HasValidLicenseKey(Guid subscriptionId)
    {
        try
        {
            var result = await _licenseService.HasValidLicenseKeyAsync(subscriptionId);
            return Ok(new ApiResponse<bool>(200, string.Empty, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}
