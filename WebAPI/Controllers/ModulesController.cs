using System;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize(Policy = "SuperAdminOnly")]
public class ModulesController : ControllerBase
{
    private readonly IModuleService _moduleService;
    private readonly ILocalizationService _localizer;

    public ModulesController(IModuleService moduleService, ILocalizationService localizer)
    {
        _moduleService = moduleService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
    {
        var (modules, meta) = await _moduleService.GetAllAsync(page, pageSize, search);
        return Ok(new { data = modules, pagination = meta });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var module = await _moduleService.GetByIdAsync(id);
        if (module == null)
            return NotFound(new { message = _localizer["Module.NotFound"] });

        return Ok(module);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModuleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var module = await _moduleService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = module.Id }, module);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateModuleDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var module = await _moduleService.UpdateAsync(id, dto);
        if (module == null)
            return NotFound(new { message = _localizer["Module.NotFound"] });

        return Ok(module);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _moduleService.DeleteAsync(id);
        return NoContent();
    }
}
