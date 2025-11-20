using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Controllers;

[Route("api/admin/auth")]
[ApiController]
/// <summary>
/// Handles login and token management for administrators.
/// </summary>
public class AdminAuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IBackupCodeService _backupCodeService;
    private readonly ICurrentUserService _currentUserService;

    public AdminAuthenticationController(
        IAuthenticationService authenticationService,
        IPasswordResetService passwordResetService,
        IBackupCodeService backupCodeService,
        ICurrentUserService currentUserService)
    {
        _authenticationService = authenticationService;
        _passwordResetService = passwordResetService;
        _backupCodeService = backupCodeService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AdminAuthenticationRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest();

        var response = await _authenticationService.AdminAuthenticationAsync(request.Username, request.Password);

        if (response.Success)
        {
            return Ok(response);
        }

        // If 2FA is required, return 200 OK with Requires2FA flag
        if (response.Requires2FA)
        {
            return Ok(response);
        }

        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized,
            response.ErrorMessage));
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authenticationService.Verify2FAAsync(
            request.Username, 
            request.Password, 
            request.TwoFactorCode);

        if (response.Success)
        {
            return Ok(response);
        }

        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized,
            response.ErrorMessage));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ApiResponse<string>(StatusCodes.Status400BadRequest,
                "Refresh token is required"));
        }

        var response = await _authenticationService.RegenerateAccessToken(request);

        if (response.Success)
        {
            return Ok(response);
        }

        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized,
            response.ErrorMessage));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _currentUserService.UserId;
        await _authenticationService.Logout(userId);
        return NoContent();
    }

    /// <summary>
    /// Request password reset OTP to be sent to email
    /// Rate limit: Max 3 requests per hour per email
    /// Use this endpoint when 2FA is NOT enabled
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var message = await _passwordResetService.SendPasswordResetOtpAsync(request, ipAddress);

        return Ok(new ApiResponse<string>(StatusCodes.Status200OK, message));
    }

    /// <summary>
    /// Request password reset OTP with 2FA verification
    /// Required when user has 2FA enabled
    /// Verifies either TwoFactorCode or BackupCode before sending reset email
    /// Rate limit: Max 3 requests per hour per email
    /// </summary>
    [HttpPost("forgot-password-with-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordWith2FA([FromBody] ForgotPasswordWith2FARequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var message = await _passwordResetService.SendPasswordResetWith2FAAsync(request, ipAddress);

        return Ok(new ApiResponse<string>(StatusCodes.Status200OK, message));
    }

    /// <summary>
    /// Check if an email address has 2FA enabled
    /// Used in forgot password flow to determine if 2FA verification is needed
    /// Returns false for non-existent emails (security: don't leak user existence)
    /// Rate Limited: 10 requests per minute to prevent email enumeration
    /// </summary>
    [HttpGet("check-2fa-status")]
    [AllowAnonymous]
    [EnableRateLimiting("Check2FAStatus")]
    public async Task<IActionResult> Check2FAStatus([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new ApiResponse<string>(StatusCodes.Status400BadRequest, "Email is required"));

        var response = await _authenticationService.Check2FAStatusAsync(email);
        return Ok(new ApiResponse<Check2FAStatusResponse>(StatusCodes.Status200OK, "2FA status retrieved", response));
    }

    /// <summary>
    /// Verify OTP code for password reset
    /// Max 5 attempts allowed
    /// </summary>
    [HttpPost("verify-reset-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetOtpRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var message = await _passwordResetService.VerifyResetOtpAsync(request);

        return Ok(new ApiResponse<string>(StatusCodes.Status200OK, message));
    }

    /// <summary>
    /// Reset password using verified OTP
    /// Invalidates all sessions for security
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var message = await _passwordResetService.ResetPasswordAsync(request);

        return Ok(new ApiResponse<string>(StatusCodes.Status200OK, message));
    }

    /// <summary>
    /// Validate magic link token from email for one-click password reset
    /// Returns OTP code for auto-fill in reset form
    /// </summary>
    [HttpPost("validate-magic-link")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateMagicLink([FromBody] ValidateMagicLinkRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _passwordResetService.ValidateMagicLinkAsync(request);

        return Ok(new ApiResponse<MagicLinkValidationResponse>(StatusCodes.Status200OK, "Token validated successfully", response));
    }

    // ============================================
    // BACKUP CODE VERIFICATION (Login Recovery)
    // ============================================
    // NOTE: Backup code management endpoints (generate, status, delete) 
    // have been moved to AdminProfileController under /api/admin/profile/me/backup-codes
    // This endpoint remains here for unauthenticated login recovery

    /// <summary>
    /// Verify backup code for 2FA authentication and issue JWT tokens
    /// Used when user lost their authenticator app during login
    /// Returns JWT access token and refresh token for login
    /// [AllowAnonymous] - User is not authenticated yet
    /// </summary>
    [HttpPost("verify-backup-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyBackupCode([FromBody] VerifyBackupCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _backupCodeService.VerifyBackupCodeAsync(request);

        if (response.Success)
        {
            return Ok(response);
        }

        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized, response.ErrorMessage));
    }
}
