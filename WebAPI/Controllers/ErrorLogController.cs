using System;
using System.Threading.Tasks;
using Application.DTOs.Logging;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for viewing error logs.
/// </summary>
[ApiController]
[Route("api/errors")]
[Authorize(Policy = "SuperAdminOnly")]
public class ErrorLogController : ControllerBase
{
    private readonly IErrorLogService _errorLogService;
    private readonly ILocalizationService _localizer;

    public ErrorLogController(
        IErrorLogService errorLogService,
        ILocalizationService localizer)
    {
        _errorLogService = errorLogService;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets error logs within a date range.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetErrors(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var (errors, meta) = await _errorLogService.GetErrorsAsync(fromDate, toDate, severity, page, pageSize);
            return Ok(new ApiResponse<object>(200, _localizer["ErrorLog.ErrorsRetrieved"], new { errors, pagination = meta }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Gets error details by ErrorId.
    /// </summary>
    [HttpGet("{errorId}")]
    public async Task<IActionResult> GetErrorById(string errorId)
    {
        try
        {
            var error = await _errorLogService.GetErrorByIdAsync(errorId);
            if (error == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["ErrorLog.NotFound"]));
            }

            return Ok(new ApiResponse<ErrorLogDto>(200, _localizer["ErrorLog.ErrorRetrieved"], error));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }

    /// <summary>
    /// Cleans up old error logs (keeps only last N days).
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupErrors([FromQuery] int keepDays = 90)
    {
        try
        {
            var deletedCount = await _errorLogService.CleanupOldErrorsAsync(keepDays);
            return Ok(new ApiResponse<object>(200, _localizer["ErrorLog.CleanedUp"], new { deletedCount }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>(500, ex.Message));
        }
    }
}



