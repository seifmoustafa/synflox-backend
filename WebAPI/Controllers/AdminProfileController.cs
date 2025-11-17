using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public AdminProfileController(
        IAdminProfileService profileService,
        ICurrentUserService currentUserService,
        ILocalizationService localizer)
    {
        _profileService = profileService;
        _currentUserService = currentUserService;
        _localizer = localizer;
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
    /// Change current user password
    /// </summary>
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _profileService.ChangeMyPasswordAsync(_currentUserService.UserId, request);
        return NoContent();
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
    /// </summary>
    [HttpPost("me/2fa/disable")]
    public async Task<IActionResult> Disable2FA()
    {
        await _profileService.Disable2FAAsync(_currentUserService.UserId);
        return NoContent();
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
