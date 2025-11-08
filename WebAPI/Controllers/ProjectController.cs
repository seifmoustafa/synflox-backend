using System;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = "SuperAdminOnly")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public ProjectController(
        IProjectService projectService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _projectService = projectService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _projectService.CreateProjectAsync(request);
            return CreatedAtAction(nameof(GetProject), new { id = result.Id },
                new ApiResponse<ProjectDto>(201, _localizer["Project.Created"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProjects([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var (projects, meta) = await _projectService.GetAllProjectsAsync(page, pageSize, search);
        return Ok(new ApiResponse<object>(200, string.Empty, new { projects, pagination = meta }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var decryptedId = _idEncryption.Decrypt(id);
        var project = await _projectService.GetProjectByIdAsync(decryptedId);
        if (project == null)
        {
            return NotFound(new ApiResponse<string>(404, _localizer["Project.NotFound"]));
        }
        return Ok(new ApiResponse<ProjectDto>(200, string.Empty, project));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _projectService.UpdateProjectAsync(decryptedId, request);
            if (result == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Project.NotFound"]));
            }
            return Ok(new ApiResponse<ProjectDto>(200, _localizer["Project.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var deleted = await _projectService.DeleteProjectAsync(decryptedId);
            if (!deleted)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Project.NotFound"]));
            }
            return Ok(new ApiResponse<string>(200, _localizer["Project.Deleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

