using System;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/project-modules")]
[Authorize(Policy = "SuperAdminOnly")]
public class ProjectModuleController : ControllerBase
{
    private readonly IProjectModuleService _projectModuleService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public ProjectModuleController(
        IProjectModuleService projectModuleService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _projectModuleService = projectModuleService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProjectModule([FromBody] CreateProjectModuleDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            // Mapper handles decryption of ProjectId and ModuleId automatically
            var result = await _projectModuleService.CreateProjectModuleAsync(request);
            // Return encrypted ID in response (mapper handles encryption)
            return CreatedAtAction(nameof(GetProjectModules), new { projectId = result.ProjectId },
                new ApiResponse<ProjectModuleDto>(201, _localizer["ProjectModule.Created"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectModules(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var decryptedProjectId = _idEncryption.Decrypt(projectId);
        var (projectModules, meta) = await _projectModuleService.GetModulesByProjectIdAsync(decryptedProjectId, page, pageSize);
        return Ok(new ApiResponse<object>(200, string.Empty, new { projectModules, pagination = meta }));
    }

    [HttpGet("module/{moduleId}")]
    public async Task<IActionResult> GetModuleProjects(Guid moduleId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var decryptedModuleId = _idEncryption.Decrypt(moduleId);
        var (projectModules, meta) = await _projectModuleService.GetProjectsByModuleIdAsync(decryptedModuleId, page, pageSize);
        return Ok(new ApiResponse<object>(200, string.Empty, new { projectModules, pagination = meta }));
    }

    [HttpDelete("project/{projectId}/module/{moduleId}")]
    public async Task<IActionResult> DeleteProjectModule(Guid projectId, Guid moduleId)
    {
        try
        {
            var decryptedProjectId = _idEncryption.Decrypt(projectId);
            var decryptedModuleId = _idEncryption.Decrypt(moduleId);
            var deleted = await _projectModuleService.DeleteProjectModuleAsync(decryptedProjectId, decryptedModuleId);
            if (!deleted)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["ProjectModule.NotFound"]));
            }
            return Ok(new ApiResponse<string>(200, _localizer["ProjectModule.Deleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

