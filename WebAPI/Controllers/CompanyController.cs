using System;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
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
    private readonly IIdEncryptionService _idEncryption;

    public CompanyController(
        ICompanyService companyService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _companyService = companyService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _companyService.CreateCompanyAsync(request);
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
        var decryptedId = _idEncryption.Decrypt(id);
        var company = await _companyService.GetCompanyByIdAsync(decryptedId);
        if (company == null)
        {
            return NotFound(new ApiResponse<string>(404, _localizer["Company.CompanyNotFound"]));
        }
        return Ok(new ApiResponse<CompanyDto>(200, string.Empty, company));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _companyService.UpdateCompanyAsync(decryptedId, request);
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

    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var deleted = await _companyService.DeleteCompanyAsync(decryptedId);
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
}

