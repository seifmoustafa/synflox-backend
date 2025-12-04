using System;
using System.Threading.Tasks;
using Application.DTOs.OfflineLicense;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for offline license key management.
/// Handles generation, validation, and management of encrypted license keys
/// for offline client applications.
/// </summary>
[ApiController]
[Route("api/offline-license")]
[Authorize]
public class OfflineLicenseController : ControllerBase
{
    private readonly IOfflineLicenseService _licenseService;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<OfflineLicenseController> _logger;

    public OfflineLicenseController(
        IOfflineLicenseService licenseService,
        ILocalizationService localizer,
        ILogger<OfflineLicenseController> logger)
    {
        _licenseService = licenseService;
        _localizer = localizer;
        _logger = logger;
    }

    #region Generation

    /// <summary>
    /// Generate a new offline license key for a subscription.
    /// Only SuperAdmin can generate license keys.
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <param name="request">Optional generation settings</param>
    /// <returns>Generated license key and metadata</returns>
    [HttpPost("generate/{subscriptionId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ApiResponse<GenerateLicenseResponse>>> GenerateLicenseKey(
        Guid subscriptionId,
        [FromBody] GenerateLicenseRequest? request = null)
    {
        try
        {
            request ??= new GenerateLicenseRequest();
            request.SubscriptionId = subscriptionId;

            var result = await _licenseService.GenerateLicenseKeyAsync(subscriptionId, request);
            return Ok(ApiResponse<GenerateLicenseResponse>.Success(result, result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating license key for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<GenerateLicenseResponse>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Regenerate license key for a subscription.
    /// Use after renewal, upgrade, or when entitlements change.
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <param name="request">Optional regeneration settings</param>
    /// <returns>New license key and metadata</returns>
    [HttpPost("regenerate/{subscriptionId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ApiResponse<GenerateLicenseResponse>>> RegenerateLicenseKey(
        Guid subscriptionId,
        [FromBody] GenerateLicenseRequest? request = null)
    {
        try
        {
            request ??= new GenerateLicenseRequest();
            request.SubscriptionId = subscriptionId;
            request.ForceRegenerate = true;

            var result = await _licenseService.RegenerateLicenseKeyAsync(subscriptionId, request);
            return Ok(ApiResponse<GenerateLicenseResponse>.Success(result, result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating license key for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<GenerateLicenseResponse>.Error(ex.Message));
        }
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate an offline license key.
    /// This endpoint can be used by client applications for online validation
    /// or by admins to verify a key.
    /// </summary>
    /// <param name="request">License key and optional fingerprint</param>
    /// <returns>Validation result with entitlements</returns>
    [HttpPost("validate")]
    [AllowAnonymous] // Allow anonymous for client validation
    public async Task<ActionResult<ApiResponse<ValidateLicenseResponse>>> ValidateLicenseKey(
        [FromBody] ValidateLicenseRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<ValidateLicenseResponse>.Error(_localizer["Common.InvalidRequest"]));

            var result = await _licenseService.ValidateLicenseKeyAsync(request);
            
            if (result.IsValid)
            {
                return Ok(ApiResponse<ValidateLicenseResponse>.Success(result, result.Message));
            }
            
            // Return 200 with IsValid=false so client can read the detailed error
            return Ok(ApiResponse<ValidateLicenseResponse>.Success(result, result.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license key");
            return BadRequest(ApiResponse<ValidateLicenseResponse>.Error(_localizer["OfflineLicense.ValidationError"]));
        }
    }

    /// <summary>
    /// Quick license key check (valid/invalid only).
    /// Lightweight endpoint for simple validation.
    /// NOTE: Uses POST to avoid license key exposure in URLs/logs.
    /// </summary>
    /// <param name="request">Request containing the license key</param>
    /// <returns>Boolean indicating if key is valid</returns>
    [HttpPost("check")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> CheckLicenseKey([FromBody] QuickCheckRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.LicenseKey))
                return BadRequest(ApiResponse<bool>.Error(_localizer["OfflineLicense.KeyRequired"]));

            var isValid = await _licenseService.IsLicenseKeyValidAsync(request.LicenseKey);
            return Ok(ApiResponse<bool>.Success(isValid, 
                isValid ? _localizer["OfflineLicense.Valid"] : _localizer["OfflineLicense.Invalid"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking license key");
            return BadRequest(ApiResponse<bool>.Error(_localizer["OfflineLicense.ValidationError"]));
        }
    }

    #endregion

    #region Management

    /// <summary>
    /// Revoke a license key (invalidate immediately).
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <param name="reason">Optional reason for revocation</param>
    /// <returns>Success status</returns>
    [HttpDelete("revoke/{subscriptionId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ApiResponse<bool>>> RevokeLicenseKey(
        Guid subscriptionId,
        [FromQuery] string? reason = null)
    {
        try
        {
            var result = await _licenseService.RevokeLicenseKeyAsync(subscriptionId, reason);
            return Ok(ApiResponse<bool>.Success(result, _localizer["OfflineLicense.RevokedSuccessfully"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking license key for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Get license information for a subscription.
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <returns>License details</returns>
    [HttpGet("subscription/{subscriptionId}")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<ActionResult<ApiResponse<OfflineLicenseDto>>> GetLicenseInfo(Guid subscriptionId)
    {
        try
        {
            var result = await _licenseService.GetLicenseInfoAsync(subscriptionId);
            if (result == null)
                return NotFound(ApiResponse<OfflineLicenseDto>.Error(_localizer["Subscription.NotFound"]));

            return Ok(ApiResponse<OfflineLicenseDto>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license info for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<OfflineLicenseDto>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Get all licenses for a company.
    /// </summary>
    /// <param name="companyId">Encrypted company ID</param>
    /// <returns>Company license summary</returns>
    [HttpGet("company/{companyId}")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<ActionResult<ApiResponse<CompanyLicenseSummaryDto>>> GetCompanyLicenses(Guid companyId)
    {
        try
        {
            var result = await _licenseService.GetCompanyLicensesAsync(companyId);
            return Ok(ApiResponse<CompanyLicenseSummaryDto>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting licenses for company {CompanyId}", companyId);
            return BadRequest(ApiResponse<CompanyLicenseSummaryDto>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Check if a subscription has a valid license key.
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <returns>True if has valid key</returns>
    [HttpGet("has-key/{subscriptionId}")]
    [Authorize(Policy = "AdminOrSuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> HasValidLicenseKey(Guid subscriptionId)
    {
        try
        {
            var result = await _licenseService.HasValidLicenseKeyAsync(subscriptionId);
            return Ok(ApiResponse<bool>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking license key for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
    }

    #endregion

    #region Download

    /// <summary>
    /// Download license key as a .lic file.
    /// Useful for distributing license keys to customers.
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <returns>License key file</returns>
    [HttpGet("download/{subscriptionId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DownloadLicenseKey(Guid subscriptionId)
    {
        try
        {
            var licenseInfo = await _licenseService.GetLicenseInfoAsync(subscriptionId);
            if (licenseInfo == null || string.IsNullOrEmpty(licenseInfo.LicenseKey))
                return NotFound(ApiResponse<object>.Error(_localizer["OfflineLicense.NoKeyExists"]));

            // Sanitize filename to prevent path traversal attacks
            var safeCompanyName = SanitizeFileName(licenseInfo.CompanyName);
            var safePlanName = SanitizeFileName(licenseInfo.PlanName);
            var fileName = $"{safeCompanyName}_{safePlanName}.lic";
            var content = System.Text.Encoding.UTF8.GetBytes(licenseInfo.LicenseKey);

            return File(content, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading license key for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<object>.Error(ex.Message));
        }
    }

    #endregion

    #region Machine Management

    /// <summary>
    /// Add an authorized machine to a license.
    /// </summary>
    /// <param name="subscriptionId">Encrypted subscription ID</param>
    /// <param name="fingerprint">Machine fingerprint to add</param>
    /// <returns>Updated license</returns>
    [HttpPost("{subscriptionId}/machines")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<ActionResult<ApiResponse<GenerateLicenseResponse>>> AddAuthorizedMachine(
        Guid subscriptionId,
        [FromBody] MachineFingerprint fingerprint)
    {
        try
        {
            if (!fingerprint.HasMinimumIdentifiers())
                return BadRequest(ApiResponse<GenerateLicenseResponse>.Error(_localizer["OfflineLicense.InsufficientFingerprint"]));

            var result = await _licenseService.AddAuthorizedMachineAsync(subscriptionId, fingerprint);
            return Ok(ApiResponse<GenerateLicenseResponse>.Success(result, _localizer["OfflineLicense.MachineAdded"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding machine to subscription {SubscriptionId}", subscriptionId);
            return BadRequest(ApiResponse<GenerateLicenseResponse>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Compute machine fingerprint hash.
    /// Utility endpoint for clients to compute their fingerprint.
    /// </summary>
    /// <param name="fingerprint">Machine fingerprint data</param>
    /// <returns>Computed hash</returns>
    [HttpPost("compute-fingerprint")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<string>> ComputeFingerprint([FromBody] MachineFingerprint fingerprint)
    {
        try
        {
            if (!fingerprint.HasMinimumIdentifiers())
                return BadRequest(ApiResponse<string>.Error(_localizer["OfflineLicense.InsufficientFingerprint"]));

            var hash = _licenseService.ComputeFingerprintHash(fingerprint);
            return Ok(ApiResponse<string>.Success(hash));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing fingerprint");
            return BadRequest(ApiResponse<string>.Error(_localizer["OfflineLicense.FingerprintError"]));
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Sanitize a string for use in filenames to prevent path traversal attacks.
    /// </summary>
    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Unknown";

        // Remove/replace invalid filename characters and path separators
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = new System.Text.StringBuilder();
        
        foreach (var c in input)
        {
            if (Array.IndexOf(invalidChars, c) < 0 && c != '.' && c != '/')
                sanitized.Append(c == ' ' ? '_' : c);
        }

        var result = sanitized.ToString();
        
        // Prevent empty result
        if (string.IsNullOrWhiteSpace(result))
            return "Unknown";
            
        // Limit length
        return result.Length > 50 ? result[..50] : result;
    }

    #endregion
}
