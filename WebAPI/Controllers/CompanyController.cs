using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Application.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly ICompanyCustomFieldService _customFieldService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public CompanyController(
        ICompanyService companyService,
        ICompanyCustomFieldService customFieldService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _companyService = companyService;
        _customFieldService = customFieldService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    /// <summary>
    /// Creates a new company
    /// </summary>
    /// <param name="request">Company creation data</param>
    /// <returns>Created company with encrypted ID</returns>
    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    [SwaggerResponse(201, "Company created successfully", typeof(ApiResponse<CompanyDto>))]
    [SwaggerResponse(400, "Bad request - validation errors", typeof(ApiResponse<string>))]
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

    /// <summary>
    /// Gets all companies with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="search">Search term (optional)</param>
    /// <returns>List of companies with pagination metadata</returns>
    [HttpGet]
    [SwaggerResponse(200, "Companies retrieved successfully", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetAllCompanies([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var (companies, meta) = await _companyService.GetAllCompaniesAsync(page, pageSize, search);
        return Ok(new ApiResponse<object>(200, string.Empty, new { companies, pagination = meta }));
    }

    /// <summary>
    /// Gets a company by ID
    /// </summary>
    /// <param name="id">Encrypted company ID</param>
    /// <returns>Company details</returns>
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Company retrieved successfully", typeof(ApiResponse<CompanyDto>))]
    [SwaggerResponse(404, "Company not found", typeof(ApiResponse<string>))]
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

    [HttpPost("bulk-delete")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkOperationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null || request.CompanyIds == null || request.CompanyIds.Count == 0)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidRequest"]));

        if (request.Action != BulkOperationAction.Delete)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidAction"]));

        try
        {
            var decryptedIds = request.CompanyIds.Select(id => _idEncryption.Decrypt(id)).ToList();
            var result = await _companyService.BulkDeleteAsync(decryptedIds);
            return Ok(new ApiResponse<BulkOperationResponse>(200, _localizer["BulkOperation.Completed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpPost("bulk-update")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> BulkUpdate([FromBody] BulkUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null || request.CompanyIds == null || request.CompanyIds.Count == 0)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.InvalidRequest"]));

        if (request.UpdateDto == null)
            return BadRequest(new ApiResponse<string>(400, _localizer["BulkOperation.UpdateDtoRequired"]));

        try
        {
            var decryptedIds = request.CompanyIds.Select(id => _idEncryption.Decrypt(id)).ToList();
            var result = await _companyService.BulkUpdateAsync(decryptedIds, request.UpdateDto);
            return Ok(new ApiResponse<BulkOperationResponse>(200, _localizer["BulkOperation.Completed"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets all custom fields for a company.
    /// </summary>
    [HttpGet("{id}/custom-fields")]
    public async Task<IActionResult> GetCustomFields(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var fields = await _customFieldService.GetFieldsByCompanyIdAsync(decryptedId);
            return Ok(new ApiResponse<object>(200, string.Empty, new { fields }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Creates a custom field for a company.
    /// </summary>
    [HttpPost("{id}/custom-fields")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateCustomField(Guid id, [FromBody] CreateCompanyCustomFieldDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            // Override CompanyId from route parameter (ignore value from body if sent)
            request.CompanyId = decryptedId;
            var result = await _customFieldService.CreateFieldAsync(request);
            return CreatedAtAction(nameof(GetCustomField), new { id, fieldId = result.Id },
                new ApiResponse<CompanyCustomFieldDto>(201, _localizer["CompanyCustomField.Created"], result));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ApiResponse<string>(404, ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets a custom field by ID.
    /// </summary>
    [HttpGet("{id}/custom-fields/{fieldId}")]
    public async Task<IActionResult> GetCustomField(Guid id, Guid fieldId)
    {
        try
        {
            var decryptedFieldId = _idEncryption.Decrypt(fieldId);
            var field = await _customFieldService.GetFieldByIdAsync(decryptedFieldId);
            if (field == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["CompanyCustomField.NotFound"]));
            }
            return Ok(new ApiResponse<CompanyCustomFieldDto>(200, string.Empty, field));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Updates a custom field.
    /// </summary>
    [HttpPut("{id}/custom-fields/{fieldId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> UpdateCustomField(Guid id, Guid fieldId, [FromBody] UpdateCompanyCustomFieldDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedFieldId = _idEncryption.Decrypt(fieldId);
            var result = await _customFieldService.UpdateFieldAsync(decryptedFieldId, request);
            return Ok(new ApiResponse<CompanyCustomFieldDto>(200, _localizer["CompanyCustomField.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Deletes a custom field.
    /// </summary>
    [HttpDelete("{id}/custom-fields/{fieldId}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteCustomField(Guid id, Guid fieldId)
    {
        try
        {
            var decryptedFieldId = _idEncryption.Decrypt(fieldId);
            await _customFieldService.DeleteFieldAsync(decryptedFieldId);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyCustomField.Deleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Exports companies to CSV or Excel format.
    /// </summary>
    [HttpGet("export")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ExportCompanies([FromQuery] string format = "csv")
    {
        try
        {
            var (companies, _) = await _companyService.GetAllCompaniesAsync(1, int.MaxValue);
            var companiesList = companies.ToList();

            byte[] fileData;
            string contentType;
            string fileExtension;

            if (format.ToLowerInvariant() == "excel")
            {
                var exportService = HttpContext.RequestServices.GetRequiredService<IExportService>();
                fileData = await exportService.ExportCompaniesToExcelAsync(companiesList);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileExtension = "xlsx";
            }
            else
            {
                var exportService = HttpContext.RequestServices.GetRequiredService<IExportService>();
                fileData = await exportService.ExportCompaniesToCsvAsync(companiesList);
                contentType = "text/csv";
                fileExtension = "csv";
            }

            var fileName = $"companies_export_{DateTime.UtcNow:yyyyMMddHHmmss}.{fileExtension}";
            return File(fileData, contentType, fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Imports companies from CSV or Excel file.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ImportCompanies(IFormFile file, [FromQuery] string format = "csv")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse<string>(400, _localizer["Import.FileRequired"]));

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var fileData = stream.ToArray();

            var importService = HttpContext.RequestServices.GetRequiredService<IImportService>();
            ImportResult<CreateCompanyDto> result;

            if (format.ToLowerInvariant() == "excel")
            {
                result = await importService.ImportCompaniesFromExcelAsync(fileData);
            }
            else
            {
                result = await importService.ImportCompaniesFromCsvAsync(fileData);
            }

            // Import valid items
            int importedCount = 0;
            foreach (var item in result.ValidItems)
            {
                try
                {
                    await _companyService.CreateCompanyAsync(item);
                    importedCount++;
                }
                catch
                {
                    // Skip individual errors, they're already in result.Errors
                }
            }

            return Ok(new ApiResponse<object>(200, _localizer["Import.Completed"], new
            {
                totalRows = result.TotalRows,
                imported = importedCount,
                errors = result.Errors,
                errorCount = result.ErrorCount
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

