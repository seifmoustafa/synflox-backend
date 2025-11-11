using System;
using System.Threading.Tasks;
using Application.DTOs.Module;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize(Policy = "SuperAdminOnly")]
public class ModuleController : ControllerBase
{
    private readonly IModuleService _moduleService;
    private readonly ILocalizationService _localizer;
    private readonly IIdEncryptionService _idEncryption;

    public ModuleController(
        IModuleService moduleService,
        ILocalizationService localizer,
        IIdEncryptionService idEncryption)
    {
        _moduleService = moduleService;
        _localizer = localizer;
        _idEncryption = idEncryption;
    }

    [HttpPost]
    public async Task<IActionResult> CreateModule([FromBody] CreateModuleDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _moduleService.CreateModuleAsync(request);
            return CreatedAtAction(nameof(GetModule), new { id = result.Id },
                new ApiResponse<ModuleDto>(201, _localizer["Module.Created"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllModules([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var (modules, meta) = await _moduleService.GetAllModulesAsync(page, pageSize, search);
        return Ok(new ApiResponse<object>(200, string.Empty, new { modules, pagination = meta }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetModule(Guid id)
    {
        var decryptedId = _idEncryption.Decrypt(id);
        var module = await _moduleService.GetModuleByIdAsync(decryptedId);
        if (module == null)
        {
            return NotFound(new ApiResponse<string>(404, _localizer["Module.NotFound"]));
        }
        return Ok(new ApiResponse<ModuleDto>(200, string.Empty, module));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateModule(Guid id, [FromBody] UpdateModuleDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var result = await _moduleService.UpdateModuleAsync(decryptedId, request);
            if (result == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Module.NotFound"]));
            }
            return Ok(new ApiResponse<ModuleDto>(200, _localizer["Module.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteModule(Guid id)
    {
        try
        {
            var decryptedId = _idEncryption.Decrypt(id);
            var deleted = await _moduleService.DeleteModuleAsync(decryptedId);
            if (!deleted)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["Module.NotFound"]));
            }
            return Ok(new ApiResponse<string>(200, _localizer["Module.Deleted"]));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

