using Application.DTOs.CompanyAdmin;
using Application.DTOs.Responses;
using Application.Services;
using Infrastructure.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for managing company administrator accounts.
/// Used by SYNFLOX admins to create and manage client admin accounts.
/// </summary>
[Route("api/admin/company-admins")]
[ApiController]
[Authorize(Policy = "SuperAdminOnly")]
public class CompanyAdminController : ControllerBase
{
    private readonly ICompanyAdminService _service;
    private readonly ILogger<CompanyAdminController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CompanyAdminController(
        ICompanyAdminService service,
        ILogger<CompanyAdminController> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _service = service;
        _logger = logger;
        _localizer = localizer;
    }

    /// <summary>
    /// Get all company admins (paginated).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var (items, totalCount) = await _service.GetAllAsync(page, pageSize, search, isActive);
            
            var result = new
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(ApiResponse<object>.Success(result, "Company admins retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company admins");
            return StatusCode(500, ApiResponse<object>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Get company admin by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CompanyAdminDetailsDto>>> GetById(Guid id)
    {
        try
        {
            var request = new GetCompanyAdminByIdRequest { AdminId = id };
            var admin = await _service.GetByIdAsync(request);
            if (admin == null)
                return NotFound(ApiResponse<CompanyAdminDetailsDto>.Error("Company admin not found"));

            return Ok(ApiResponse<CompanyAdminDetailsDto>.Success(admin, "Company admin retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<CompanyAdminDetailsDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Get company admin by company ID.
    /// </summary>
    [HttpGet("by-company/{companyId}")]
    public async Task<ActionResult<ApiResponse<CompanyAdminDetailsDto>>> GetByCompanyId(Guid companyId)
    {
        try
        {
            var request = new GetCompanyAdminByCompanyIdRequest { CompanyId = companyId };
            var admin = await _service.GetByCompanyIdAsync(request);
            if (admin == null)
                return NotFound(ApiResponse<CompanyAdminDetailsDto>.Error("No admin found for this company"));

            return Ok(ApiResponse<CompanyAdminDetailsDto>.Success(admin, "Company admin retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company admin for company {CompanyId}", companyId);
            return StatusCode(500, ApiResponse<CompanyAdminDetailsDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Create a new company admin account.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompanyAdminDto>>> Create([FromBody] CreateCompanyAdminRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CompanyAdminDto>.Error("Invalid request data"));

            var admin = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = admin.Id }, 
                ApiResponse<CompanyAdminDto>.Success(admin, "Company admin created successfully"));
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return BadRequest(ApiResponse<CompanyAdminDto>.Error(ex.Message));
        }
        catch (Domain.Exceptions.NotFoundException ex)
        {
            return NotFound(ApiResponse<CompanyAdminDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company admin");
            return StatusCode(500, ApiResponse<CompanyAdminDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Update company admin account.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CompanyAdminDto>>> Update(Guid id, [FromBody] UpdateCompanyAdminRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CompanyAdminDto>.Error("Invalid request data"));

            var idRequest = new UpdateCompanyAdminByIdRequest { AdminId = id };
            var admin = await _service.UpdateAsync(idRequest, request);
            return Ok(ApiResponse<CompanyAdminDto>.Success(admin, "Company admin updated successfully"));
        }
        catch (Domain.Exceptions.NotFoundException ex)
        {
            return NotFound(ApiResponse<CompanyAdminDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<CompanyAdminDto>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Delete (soft delete) company admin account.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            var request = new DeleteCompanyAdminRequest { AdminId = id };
            var result = await _service.DeleteAsync(request);
            return Ok(ApiResponse<bool>.Success(result, "Company admin deleted successfully"));
        }
        catch (Domain.Exceptions.NotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Reset admin password.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(Guid id, [FromBody] ResetAdminPasswordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<bool>.Error("Invalid request data"));

            var idRequest = new ResetPasswordByIdRequest { AdminId = id };
            var result = await _service.ResetPasswordAsync(idRequest, request);
            return Ok(ApiResponse<bool>.Success(result, "Password reset successfully"));
        }
        catch (Domain.Exceptions.NotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Unlock admin account.
    /// </summary>
    [HttpPost("{id}/unlock")]
    public async Task<ActionResult<ApiResponse<bool>>> UnlockAccount(Guid id)
    {
        try
        {
            var request = new UnlockAccountByIdRequest { AdminId = id };
            var result = await _service.UnlockAccountAsync(request);
            return Ok(ApiResponse<bool>.Success(result, "Account unlocked successfully"));
        }
        catch (Domain.Exceptions.NotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Terminate all sessions for an admin.
    /// </summary>
    [HttpPost("{id}/terminate-sessions")]
    public async Task<ActionResult<ApiResponse<int>>> TerminateSessions(Guid id, [FromQuery] string? reason = null)
    {
        try
        {
            var idRequest = new TerminateSessionsByIdRequest { AdminId = id };
            var count = await _service.TerminateAllSessionsAsync(idRequest, reason);
            return Ok(ApiResponse<int>.Success(count, $"Terminated {count} session(s)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating sessions for company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<int>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Check if username is available.
    /// </summary>
    [HttpGet("check-username")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckUsername(
        [FromQuery] string username,
        [FromQuery] Guid? excludeAdminId = null)
    {
        try
        {
            var available = await _service.IsUsernameAvailableAsync(username, excludeAdminId);
            return Ok(ApiResponse<bool>.Success(available, available ? "Username is available" : "Username is taken"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username availability");
            return StatusCode(500, ApiResponse<bool>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Get company admin by company ID (returns admin data or null if none exists).
    /// This endpoint replaces the old company-has-admin boolean check.
    /// </summary>
    [HttpGet("company-has-admin/{companyId}")]
    public async Task<ActionResult<ApiResponse<CompanyAdminDetailsDto?>>> CompanyHasAdmin(Guid companyId)
    {
        try
        {
            var request = new GetCompanyAdminByCompanyIdRequest { CompanyId = companyId };
            var admin = await _service.GetByCompanyIdAsync(request);
            
            if (admin == null)
            {
                return Ok(ApiResponse<CompanyAdminDetailsDto?>.Success(null, _localizer["CompanyAdmin.NoAdmin"]));
            }
            
            return Ok(ApiResponse<CompanyAdminDetailsDto?>.Success(admin, _localizer["CompanyAdmin.Retrieved"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company admin for company {CompanyId}", companyId);
            return StatusCode(500, ApiResponse<CompanyAdminDetailsDto?>.Error(_localizer["Common.InternalError"]));
        }
    }

    /// <summary>
    /// Get admin's session history.
    /// </summary>
    [HttpGet("{id}/sessions")]
    public async Task<ActionResult<ApiResponse<List<CompanyAdminSessionDto>>>> GetSessionHistory(
        Guid id,
        [FromQuery] int limit = 50)
    {
        try
        {
            var sessions = await _service.GetSessionHistoryAsync(id, limit);
            return Ok(ApiResponse<List<CompanyAdminSessionDto>>.Success(sessions, "Session history retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session history for company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<List<CompanyAdminSessionDto>>.Error("Internal server error"));
        }
    }

    /// <summary>
    /// Get admin's active sessions.
    /// </summary>
    [HttpGet("{id}/active-sessions")]
    public async Task<ActionResult<ApiResponse<List<CompanyAdminSessionDto>>>> GetActiveSessions(Guid id)
    {
        try
        {
            var sessions = await _service.GetActiveSessionsAsync(id);
            return Ok(ApiResponse<List<CompanyAdminSessionDto>>.Success(sessions, "Active sessions retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions for company admin {AdminId}", id);
            return StatusCode(500, ApiResponse<List<CompanyAdminSessionDto>>.Error("Internal server error"));
        }
    }
}
