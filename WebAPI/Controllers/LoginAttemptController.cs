using System;
using System.Threading.Tasks;
using Application.DTOs.Authentication;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/login-attempts")]
[Authorize(Policy = "SuperAdminOnly")]
public class LoginAttemptController : ControllerBase
{
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ILocalizationService _localizer;

    public LoginAttemptController(
        ILoginAttemptService loginAttemptService,
        ILocalizationService localizer)
    {
        _loginAttemptService = loginAttemptService;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets all login attempts with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAttempts(
        [FromQuery] string? username,
        [FromQuery] bool? success,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var (attempts, meta) = await _loginAttemptService.GetAllAttemptsAsync(
                username, success, fromDate, toDate, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { attempts, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets recent failed login attempts for a username.
    /// </summary>
    [HttpGet("username/{username}/failed")]
    public async Task<IActionResult> GetRecentFailedAttempts(
        string username,
        [FromQuery] int minutes = 30)
    {
        try
        {
            var attempts = await _loginAttemptService.GetRecentFailedAttemptsAsync(username, minutes);
            return Ok(new ApiResponse<object>(200, string.Empty, new { attempts }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

