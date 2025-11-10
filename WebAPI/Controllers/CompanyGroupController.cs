using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for managing company groups.
/// </summary>
[ApiController]
[Route("api/company-groups")]
[Authorize(Policy = "SuperAdminOnly")]
public class CompanyGroupController : ControllerBase
{
    private readonly ICompanyGroupService _groupService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public CompanyGroupController(
        ICompanyGroupService groupService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _groupService = groupService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    /// <summary>
    /// Creates a new company group.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateCompanyGroupDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _groupService.CreateGroupAsync(request);
            return CreatedAtAction(nameof(GetGroupById), new { id = result.Id },
                new ApiResponse<CompanyGroupDto>(201, _localizer["CompanyGroup.Created"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets all company groups.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            var (groups, meta) = await _groupService.GetAllGroupsAsync(page, pageSize, search);
            return Ok(new ApiResponse<object>(200, string.Empty, new { groups, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets a company group by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroupById(string id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            var group = await _groupService.GetGroupByIdAsync(decryptedId);
            if (group == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["CompanyGroup.NotFound"]));
            }

            return Ok(new ApiResponse<CompanyGroupDto>(200, string.Empty, group));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Updates a company group.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(string id, [FromBody] UpdateCompanyGroupDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            var result = await _groupService.UpdateGroupAsync(decryptedId, request);
            return Ok(new ApiResponse<CompanyGroupDto>(200, _localizer["CompanyGroup.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Deletes a company group.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(string id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            await _groupService.DeleteGroupAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyGroup.Deleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Adds companies to a group.
    /// </summary>
    [HttpPost("{id}/companies")]
    public async Task<IActionResult> AddCompaniesToGroup(string id, [FromBody] List<Guid> companyIds)
    {
        try
        {
            var decryptedGroupId = _idEncryption.Decrypt(Guid.Parse(id));
            var decryptedCompanyIds = companyIds.Select(c => _idEncryption.Decrypt(c)).ToList();
            await _groupService.AddCompaniesToGroupAsync(decryptedGroupId, decryptedCompanyIds);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyGroup.CompaniesAdded"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Removes companies from a group.
    /// </summary>
    [HttpDelete("{id}/companies")]
    public async Task<IActionResult> RemoveCompaniesFromGroup(string id, [FromBody] List<Guid> companyIds)
    {
        try
        {
            var decryptedGroupId = _idEncryption.Decrypt(Guid.Parse(id));
            var decryptedCompanyIds = companyIds.Select(c => _idEncryption.Decrypt(c)).ToList();
            await _groupService.RemoveCompaniesFromGroupAsync(decryptedGroupId, decryptedCompanyIds);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyGroup.CompaniesRemoved"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets companies in a group.
    /// </summary>
    [HttpGet("{id}/companies")]
    public async Task<IActionResult> GetCompaniesInGroup(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            var (companies, meta) = await _groupService.GetCompaniesInGroupAsync(decryptedId, page, pageSize);
            return Ok(new ApiResponse<object>(200, string.Empty, new { companies, pagination = meta }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets groups for a company.
    /// </summary>
    [HttpGet("company/{companyId}")]
    public async Task<IActionResult> GetGroupsForCompany(Guid companyId)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(companyId);
            var groups = await _groupService.GetGroupsForCompanyAsync(decryptedId);
            return Ok(new ApiResponse<object>(200, string.Empty, new { groups }));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Activates all companies in a group.
    /// </summary>
    [HttpPost("{id}/bulk-activate")]
    public async Task<IActionResult> BulkActivate(string id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            await _groupService.BulkActivateByGroupAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyGroup.BulkActivated"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Suspends all companies in a group.
    /// </summary>
    [HttpPost("{id}/bulk-suspend")]
    public async Task<IActionResult> BulkSuspend(string id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            await _groupService.BulkSuspendByGroupAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyGroup.BulkSuspended"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Resumes all companies in a group.
    /// </summary>
    [HttpPost("{id}/bulk-resume")]
    public async Task<IActionResult> BulkResume(string id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            await _groupService.BulkResumeByGroupAsync(decryptedId);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyGroup.BulkResumed"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Extends all companies in a group.
    /// </summary>
    [HttpPost("{id}/bulk-extend")]
    public async Task<IActionResult> BulkExtend(string id, [FromBody] DateTime expiryDate)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(Guid.Parse(id));
            await _groupService.BulkExtendByGroupAsync(decryptedId, expiryDate);
            return Ok(new ApiResponse<string>(200, _localizer["CompanyGroup.BulkExtended"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}



