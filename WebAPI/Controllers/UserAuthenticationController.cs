using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI.Controllers;

[Route("api/users/auth")]
[ApiController]
/// <summary>
/// Handles registration, login and token management for users.
/// </summary>
public class UserAuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUploadService _uploadService;

    public UserAuthenticationController(IAuthenticationService authenticationService, ICurrentUserService currentUserService, IUploadService uploadService)
    {
        _authenticationService = authenticationService;
        _currentUserService = currentUserService;
        _uploadService = uploadService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
    {
        if (request == null) return BadRequest();

        if (request.Image?.UploadId != null)
        {
            var url = await _uploadService.ConsumeAsync(request.Image.UploadId);
            if (url == null) return BadRequest();
            request.ImageUrl = url;
        }

        var result = await _authenticationService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
    {
        await _authenticationService.VerifyEmailAsync(request.Email, request.Code);
        return Ok();
    }

    [HttpPost("send-email-verification"), HttpPost("resend-email-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> SendEmailVerification([FromBody] ResendEmailVerificationOtpRequest request)
    {
        var result = await _authenticationService.ResendVerificationCodeAsync(request.Email);
        if (!result.Sent)
        {
            Response.Headers["Retry-After"] = result.RetryAfterSeconds?.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new ApiResponse<string>(StatusCodes.Status429TooManyRequests,
                    result.Message ?? string.Empty));
        }
        return Accepted(new ApiResponse<string>(StatusCodes.Status202Accepted, string.Empty));
    }

    [HttpPost("send-phone-verification"), HttpPost("resend-phone-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> SendPhoneVerification([FromBody] ResendPhoneVerificationOtpRequest request)
    {
        var result = await _authenticationService.ResendPhoneVerificationCodeAsync(request.PhoneNumber);
        if (!result.Sent)
        {
            Response.Headers["Retry-After"] = result.RetryAfterSeconds?.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new ApiResponse<string>(StatusCodes.Status429TooManyRequests,
                    result.Message ?? string.Empty));
        }
        return Accepted(new ApiResponse<string>(StatusCodes.Status202Accepted, string.Empty));
    }

    [HttpPost("verify-phone")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPhone([FromBody] PhoneVerificationRequest request)
    {
        await _authenticationService.VerifyPhoneAsync(request.PhoneNumber, request.Code);
        return Ok();
    }

    [HttpPost("request-password-reset")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetOtpRequest request)
    {
        var result = await _authenticationService.SendPasswordResetCodeAsync(request.Email);
        if (!result.Sent)
        {
            Response.Headers["Retry-After"] = result.RetryAfterSeconds?.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new ApiResponse<string>(StatusCodes.Status429TooManyRequests,
                    result.Message ?? string.Empty));
        }
        return Accepted(new ApiResponse<string>(StatusCodes.Status202Accepted, string.Empty));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithOtpRequest request)
    {
        await _authenticationService.ResetPasswordWithCodeAsync(request.Email, request.Code, request.NewPassword);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    /// <summary>
    /// Sign in using email, phone number or username and password.
    /// </summary>
    public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
    {
        if (request == null || request.Credential.IsNullOrEmpty() || request.Password.IsNullOrEmpty()) return BadRequest();

        var response = await _authenticationService.AuthenticationAsync(request.Credential, request.Password);

        if (response.Success)
        {
            return Ok(response);
        }

        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized,
            response.ErrorMessage));
    }

    [HttpPost("login/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLogin(string provider, [FromBody] ExternalAuthRequest request)
    {
        var response = await _authenticationService.ExternalLoginAsync(provider?.ToLowerInvariant(), request);
        if (response.Success)
        {
            return Ok(response);
        }
        return Unauthorized(new ApiResponse<string>(StatusCodes.Status401Unauthorized,
            response.ErrorMessage));
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        Guid accountId = _currentUserService.UserId;

        if (accountId == Guid.Empty) return BadRequest();

        var response = await _authenticationService.RegenerateAccessToken(accountId);

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
}
