using Application.DTOs.CompanyAdmin;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Authentication controller for company administrators.
/// Used by client admin to login, logout, and manage their session.
/// </summary>
[Route("api/client/admin-auth")]
[ApiController]
[AllowAnonymous]
public class CompanyAdminAuthController : ControllerBase
{
    private readonly ICompanyAdminService _service;
    private readonly ILogger<CompanyAdminAuthController> _logger;

    public CompanyAdminAuthController(
        ICompanyAdminService service,
        ILogger<CompanyAdminAuthController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Login with username and password.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AdminLoginResponse>>> Login([FromBody] AdminLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AdminLoginResponse>.Error("Invalid request data"));

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _service.LoginAsync(request, ipAddress);

            if (!response.Success)
            {
                return Unauthorized(ApiResponse<AdminLoginResponse>.Error(response.Message ?? "Login failed"));
            }

            return Ok(ApiResponse<AdminLoginResponse>.Success(response, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin login");
            return StatusCode(500, ApiResponse<AdminLoginResponse>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Logout from current session.
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout([FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<bool>.Error("Session ID is required"));

            var result = await _service.LogoutAsync(sessionId);
            return Ok(ApiResponse<bool>.Success(result, result ? "Logged out successfully" : "Session not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin logout");
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Validate session and get admin info.
    /// </summary>
    [HttpGet("validate")]
    public async Task<ActionResult<ApiResponse<CompanyAdminDetailsDto>>> ValidateSession(
        [FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<CompanyAdminDetailsDto>.Error("Session ID is required"));

            var admin = await _service.ValidateSessionAsync(sessionId);
            if (admin == null)
                return Unauthorized(ApiResponse<CompanyAdminDetailsDto>.Error("Invalid or expired session"));

            // Refresh session activity
            await _service.RefreshSessionAsync(sessionId);

            return Ok(ApiResponse<CompanyAdminDetailsDto>.Success(admin, "Session valid"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating admin session");
            return StatusCode(500, ApiResponse<CompanyAdminDetailsDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Refresh session (heartbeat).
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<bool>>> RefreshSession(
        [FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<bool>.Error("Session ID is required"));

            var result = await _service.RefreshSessionAsync(sessionId);
            if (!result)
                return Unauthorized(ApiResponse<bool>.Error("Invalid or expired session"));

            return Ok(ApiResponse<bool>.Success(true, "Session refreshed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing admin session");
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Change password (requires current password).
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        [FromBody] CompanyAdminChangePasswordRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<bool>.Error("Session ID is required"));

            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.Error("Invalid request data"));

            var admin = await _service.ValidateSessionAsync(sessionId);
            if (admin == null)
                return Unauthorized(ApiResponse<bool>.Error("Invalid or expired session"));

            var result = await _service.ChangePasswordAsync(admin.Id, request);
            return Ok(ApiResponse<bool>.Success(result, "Password changed successfully"));
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing admin password");
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Update session settings.
    /// </summary>
    [HttpPut("session-settings")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSessionSettings(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        [FromBody] UpdateSessionSettingsRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<bool>.Error("Session ID is required"));

            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.Error("Invalid request data"));

            var admin = await _service.ValidateSessionAsync(sessionId);
            if (admin == null)
                return Unauthorized(ApiResponse<bool>.Error("Invalid or expired session"));

            var result = await _service.UpdateSessionSettingsAsync(admin.Id, request);
            return Ok(ApiResponse<bool>.Success(result, "Session settings updated"));
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return BadRequest(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session settings");
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Get current admin's active sessions.
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<CompanyAdminSessionDto>>>> GetMySessions(
        [FromHeader(Name = "X-Session-Id")] string? sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<List<CompanyAdminSessionDto>>.Error("Session ID is required"));

            var admin = await _service.ValidateSessionAsync(sessionId);
            if (admin == null)
                return Unauthorized(ApiResponse<List<CompanyAdminSessionDto>>.Error("Invalid or expired session"));

            var sessions = await _service.GetActiveSessionsAsync(admin.Id);
            return Ok(ApiResponse<List<CompanyAdminSessionDto>>.Success(sessions, "Sessions retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin sessions");
            return StatusCode(500, ApiResponse<List<CompanyAdminSessionDto>>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Terminate a specific session.
    /// </summary>
    [HttpDelete("sessions/{targetSessionId}")]
    public async Task<ActionResult<ApiResponse<bool>>> TerminateSession(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        string targetSessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<bool>.Error("Session ID is required"));

            var admin = await _service.ValidateSessionAsync(sessionId);
            if (admin == null)
                return Unauthorized(ApiResponse<bool>.Error("Invalid or expired session"));

            var result = await _service.TerminateSessionAsync(admin.Id, targetSessionId);
            return Ok(ApiResponse<bool>.Success(result, result ? "Session terminated" : "Session not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating admin session");
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Get session history.
    /// </summary>
    [HttpGet("sessions/history")]
    public async Task<ActionResult<ApiResponse<List<CompanyAdminSessionDto>>>> GetSessionHistory(
        [FromHeader(Name = "X-Session-Id")] string? sessionId,
        [FromQuery] int limit = 50)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
                return BadRequest(ApiResponse<List<CompanyAdminSessionDto>>.Error("Session ID is required"));

            var admin = await _service.ValidateSessionAsync(sessionId);
            if (admin == null)
                return Unauthorized(ApiResponse<List<CompanyAdminSessionDto>>.Error("Invalid or expired session"));

            var sessions = await _service.GetSessionHistoryAsync(admin.Id, limit);
            return Ok(ApiResponse<List<CompanyAdminSessionDto>>.Success(sessions, "Session history retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session history");
            return StatusCode(500, ApiResponse<List<CompanyAdminSessionDto>>.Error("Internal server error"));
        }
    }
}
