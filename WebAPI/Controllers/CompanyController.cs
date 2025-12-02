using System;
using System.Threading.Tasks;
using Application.DTOs.Company;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ILocalizationService _localizer;

    public CompanyController(ICompanyService companyService, ILocalizationService localizer)
    {
        _companyService = companyService;
        _localizer = localizer;
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto request, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _companyService.CreateCompanyAsync(request, lang);
            return CreatedAtAction(nameof(GetCompany), new { id = result.Id }, 
                new ApiResponse<CompanyDto>(201, _localizer["Company.CompanyCreated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCompanies([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var (companies, meta) = await _companyService.GetAllCompaniesAsync(page, pageSize, search);
        return Ok(new ApiResponse<object>(200, string.Empty, new { companies, pagination = meta }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCompany(Guid id)
    {
        var request = new GetCompanyByIdRequest { CompanyId = id };
        var company = await _companyService.GetCompanyByIdAsync(request);
        if (company == null)
        {
            return NotFound(new ApiResponse<string>(404, _localizer["Company.CompanyNotFound"]));
        }
        return Ok(new ApiResponse<CompanyDto>(200, string.Empty, company));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyDto updateData, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (updateData == null) return BadRequest();

        try
        {
            var request = new UpdateCompanyByIdRequest { CompanyId = id, UpdateData = updateData };
            var result = await _companyService.UpdateCompanyAsync(request, lang);
            if (result == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Company.CompanyNotFound"]));
            }
            return Ok(new ApiResponse<CompanyDto>(200, _localizer["Company.CompanyUpdated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Get delete preview for a company
    /// </summary>
    [HttpGet("{id}/delete-preview")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetDeletePreview(Guid id)
    {
        var request = new GetCompanyByIdRequest { CompanyId = id };
        var preview = await _companyService.GetDeletePreviewAsync(request);
        return Ok(new ApiResponse<object>(200, _localizer["Success"], preview));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteCompany(Guid id, [FromQuery] bool confirmCascade = false, [FromQuery] string? lang = null)
    {
        try
        {
            var request = new DeleteCompanyRequest { CompanyId = id };
            var deleted = await _companyService.DeleteCompanyAsync(request, confirmCascade, lang);
            if (!deleted)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Company.CompanyNotFound"]));
            }
            return Ok(new ApiResponse<string>(200, _localizer["Company.CompanyDeleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("{id}/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ActivateCompany(Guid id, [FromBody] CompanyActionRequest request, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        // Set the ID from route parameter
        request.CompanyId = id;

        try
        {
            var result = await _companyService.ActivateCompanyAsync(request, lang);
            if (!result)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Company.CompanyNotFound"]));
            }
            return Ok(new ApiResponse<string>(200, "Company activated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeactivateCompany(Guid id, [FromBody] CompanyActionRequest request, [FromQuery] string? lang = null)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        // Set the ID from route parameter
        request.CompanyId = id;

        try
        {
            var result = await _companyService.DeactivateCompanyAsync(request, lang);
            if (!result)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Company.CompanyNotFound"]));
            }
            return Ok(new ApiResponse<string>(200, "Company deactivated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk/delete")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkDeleteCompanies([FromBody] BulkCompanyActionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _companyService.BulkDeleteCompaniesAsync(request);
            return Ok(new ApiResponse<BulkOperationResult>(200, result.Summary, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk/activate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkActivateCompanies([FromBody] BulkCompanyActionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _companyService.BulkActivateCompaniesAsync(request);
            return Ok(new ApiResponse<BulkOperationResult>(200, result.Summary, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk/deactivate")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkDeactivateCompanies([FromBody] BulkCompanyActionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _companyService.BulkDeactivateCompaniesAsync(request);
            return Ok(new ApiResponse<BulkOperationResult>(200, result.Summary, result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

