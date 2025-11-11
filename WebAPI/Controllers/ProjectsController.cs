using System;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = "SuperAdminOnly")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILocalizationService _localizer;

    public ProjectsController(IProjectService projectService, ILocalizationService localizer)
    {
        _projectService = projectService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var (projects, meta) = await _projectService.GetAllAsync(page, pageSize, search);
        return Ok(new { data = projects, pagination = meta });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
            return NotFound(new { message = _localizer["Project.NotFound"] });

        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await _projectService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await _projectService.UpdateAsync(id, dto);
        if (project == null)
            return NotFound(new { message = _localizer["Project.NotFound"] });

        return Ok(project);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _projectService.DeleteAsync(id);
        return NoContent();
    }
}
