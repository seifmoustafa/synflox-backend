using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Application.DTOs.Security;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for admin profile operations (current user only)
/// Handles profile updates, preferences, 2FA, password changes, and security settings
/// Separated from AdminManagementController for Single Responsibility Principle
/// </summary>
[ApiController]
[Route("api/admin/profile")]
[Authorize]
public class AdminProfileController : ControllerBase
{
    private readonly IAdminProfileService _profileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILocalizationService _localizer;
    private readonly IBackupCodeService _backupCodeService;
    private readonly ISecurityAnalyticsService _securityAnalyticsService;
    private readonly IAdvancedSecurityAnalyticsService _advancedAnalyticsService;
    private readonly ISecurityReportService _securityReportService;

    public AdminProfileController(
        IAdminProfileService profileService,
        ICurrentUserService currentUserService,
        ILocalizationService localizer,
        IBackupCodeService backupCodeService,
        ISecurityAnalyticsService securityAnalyticsService,
        IAdvancedSecurityAnalyticsService advancedAnalyticsService,
        ISecurityReportService securityReportService)
    {
        _profileService = profileService;
        _currentUserService = currentUserService;
        _localizer = localizer;
        _backupCodeService = backupCodeService;
        _securityAnalyticsService = securityAnalyticsService;
        _advancedAnalyticsService = advancedAnalyticsService;
        _securityReportService = securityReportService;
    }

    // ===== Profile Information =====

    /// <summary>
    /// Get current user profile with full information
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await _profileService.GetMyProfileAsync(_currentUserService.UserId);
        return profile is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(profile);
    }

    /// <summary>
    /// Get current user profile statistics
    /// </summary>
    [HttpGet("me/statistics")]
    public async Task<IActionResult> GetMyStatistics()
    {
        var stats = await _profileService.GetMyStatisticsAsync(_currentUserService.UserId);
        return Ok(stats);
    }

    // ===== Profile Updates =====

    /// <summary>
    /// Update current user basic profile information
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _profileService.UpdateMyProfileAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Update current user preferences (language, theme, timezone, etc.)
    /// </summary>
    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdateMyPreferences([FromBody] UpdatePreferencesRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _profileService.UpdateMyPreferencesAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Update current user notification preferences
    /// </summary>
    [HttpPut("me/notifications")]
    public async Task<IActionResult> UpdateMyNotifications([FromBody] UpdateNotificationPreferencesRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _profileService.UpdateMyNotificationPreferencesAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    // ===== Profile Picture =====

    /// <summary>
    /// Upload profile picture for current user
    /// </summary>
    [HttpPost("me/picture")]
    public async Task<IActionResult> UploadProfilePicture([FromBody] UploadProfilePictureRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updated = await _profileService.UploadMyProfilePictureAsync(_currentUserService.UserId, request);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    /// <summary>
    /// Delete profile picture for current user
    /// </summary>
    [HttpDelete("me/picture")]
    public async Task<IActionResult> DeleteProfilePicture()
    {
        var updated = await _profileService.DeleteMyProfilePictureAsync(_currentUserService.UserId);
        return updated is null ? NotFound(new { message = _localizer["UserNotFound"] }) : Ok(updated);
    }

    // ===== Password Management =====

    /// <summary>
    /// Change current user password (simple - no 2FA required)
    /// Use this endpoint when 2FA is NOT enabled
    /// </summary>
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _profileService.ChangeMyPasswordAsync(_currentUserService.UserId, request);
        return Ok(new { message = _localizer["Password.Changed"] ?? "Password changed successfully. All sessions have been invalidated." });
    }

    /// <summary>
    /// Change current user password with 2FA verification
    /// Required when user has 2FA enabled
    /// Accepts either TwoFactorCode or BackupCode
    /// Invalidates all refresh tokens after password change
    /// Rate Limited: 5 attempts per hour per user
    /// </summary>
    [HttpPut("me/password/change-with-2fa")]
    [EnableRateLimiting("PasswordChange")]
    public async Task<IActionResult> ChangeMyPasswordWith2FA([FromBody] ChangePasswordWith2FARequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _profileService.ChangeMyPasswordWith2FAAsync(_currentUserService.UserId, request);
        return Ok(new { message = _localizer["Password.Changed"] ?? "Password changed successfully. All sessions have been invalidated." });
    }

    // ===== Two-Factor Authentication =====

    /// <summary>
    /// Enable 2FA for current user (generates secret and QR code)
    /// </summary>
    [HttpPost("me/2fa/enable")]
    public async Task<IActionResult> Enable2FA()
    {
        var result = await _profileService.Enable2FAAsync(_currentUserService.UserId);
        return Ok(result);
    }

    /// <summary>
    /// Verify and activate 2FA for current user
    /// </summary>
    [HttpPost("me/2fa/verify")]
    public async Task<IActionResult> Verify2FA([FromBody] Enable2FARequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _profileService.Verify2FAAsync(_currentUserService.UserId, request.VerificationCode);
        return result ? Ok(new { message = _localizer["2FAEnabled"] }) : BadRequest(new { message = _localizer["Invalid2FACode"] });
    }

    /// <summary>
    /// Disable 2FA for current user
    /// Requires password confirmation for security
    /// Automatically deletes all backup codes
    /// </summary>
    [HttpPost("me/2fa/disable")]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _profileService.Disable2FAAsync(_currentUserService.UserId, request.CurrentPassword);
        return Ok(new { message = _localizer["2FADisabled"] ?? "Two-factor authentication has been disabled successfully." });
    }

    /// <summary>
    /// Reset 2FA for current user (generates new secret and QR code)
    /// Requires password confirmation for security
    /// Deletes all old backup codes - user must generate new ones
    /// Used when user loses access to authenticator app but still has account access
    /// </summary>
    [HttpPost("me/2fa/reset")]
    public async Task<IActionResult> Reset2FA([FromBody] Reset2FARequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _profileService.Reset2FAAsync(_currentUserService.UserId, request.CurrentPassword);
        return Ok(result);
    }

    // ===== Backup Codes Management =====

    /// <summary>
    /// Generate new set of 10 backup codes for 2FA recovery
    /// SECURITY: Requires current password confirmation
    /// Invalidates all previous backup codes
    /// Codes are shown ONLY once - user must save them
    /// </summary>
    [HttpPost("me/backup-codes/generate")]
    public async Task<IActionResult> GenerateBackupCodes([FromBody] GenerateBackupCodesRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _backupCodeService.GenerateBackupCodesAsync(_currentUserService.UserId, request.CurrentPassword);
        return Ok(response);
    }

    /// <summary>
    /// Get status of backup codes (count of remaining/used codes)
    /// Returns NeedsRegeneration flag when all codes are used
    /// </summary>
    [HttpGet("me/backup-codes/status")]
    public async Task<IActionResult> GetBackupCodesStatus()
    {
        var status = await _backupCodeService.GetBackupCodesStatusAsync(_currentUserService.UserId);
        return Ok(status);
    }

    /// <summary>
    /// Delete all backup codes for current user
    /// Called when regenerating codes or disabling 2FA
    /// </summary>
    [HttpDelete("me/backup-codes")]
    public async Task<IActionResult> DeleteBackupCodes()
    {
        await _backupCodeService.DeleteAllBackupCodesAsync(_currentUserService.UserId);
        return Ok(new { message = _localizer["BackupCodes.Deleted"] ?? "All backup codes deleted successfully." });
    }

    /// <summary>
    /// Export backup codes in specified format (PDF, Text, or JSON)
    /// SECURITY: Accepts codes from frontend (codes shown only once during generation)
    /// Returns base64-encoded file content ready for download
    /// Supported formats: "pdf", "text", "json"
    /// </summary>
    [HttpPost("me/backup-codes/export")]
    public async Task<IActionResult> ExportBackupCodes([FromBody] ExportBackupCodesRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _backupCodeService.ExportBackupCodesAsync(_currentUserService.UserId, request);
        return Ok(response);
    }

    // ===== Security Analytics =====

    /// <summary>
    /// Get comprehensive security dashboard for current user
    /// Includes 2FA stats, backup codes status, recent security events, failed login attempts
    /// Calculates security score and provides personalized recommendations
    /// </summary>
    [HttpGet("me/security/dashboard")]
    public async Task<IActionResult> GetSecurityDashboard()
    {
        var dashboard = await _securityAnalyticsService.GetSecurityDashboardAsync(_currentUserService.UserId);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get advanced security analytics with time-series data and trends
    /// Includes login patterns, 2FA analytics, threat assessment
    /// </summary>
    [HttpGet("me/security/analytics")]
    public async Task<IActionResult> GetAdvancedAnalytics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var analytics = await _advancedAnalyticsService.GetAdvancedAnalyticsAsync(_currentUserService.UserId, start, end);
        return Ok(analytics);
    }

    /// <summary>
    /// Export security report in specified format (PDF/Excel/JSON)
    /// Comprehensive security report with executive summary, threat assessment, recommendations
    /// Rate Limited: DEV=1000/hour | PRODUCTION=10/hour per user
    /// </summary>
    [HttpPost("me/security/report/export")]
    [EnableRateLimiting("SecurityReports")]
    public async Task<IActionResult> ExportSecurityReport([FromBody] SecurityReportRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var report = await _securityReportService.ExportSecurityReportAsync(_currentUserService.UserId, request);
        return Ok(report);
    }

    // ===== Account Management =====

    /// <summary>
    /// Delete current user account (soft delete)
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount()
    {
        await _profileService.DeleteMyAccountAsync(_currentUserService.UserId);
        return NoContent();
    }
}
