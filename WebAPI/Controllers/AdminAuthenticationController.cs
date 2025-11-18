using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ICurrentUserService _currentUserService;

    public AdminAuthenticationController(
        IAuthenticationService authenticationService,
        IPasswordResetService passwordResetService,
        ICurrentUserService currentUserService)
    {
        _authenticationService = authenticationService;
        _passwordResetService = passwordResetService;
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
}
