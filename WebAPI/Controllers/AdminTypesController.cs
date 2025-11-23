using System;
using System.Threading.Tasks;
using Application.DTOs.AdminType;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/admin-types")]
[Authorize(Policy = "SuperAdminOnly")]
public class AdminTypesController : ControllerBase
{
    private readonly IAdminTypeService _service;

    public AdminTypesController(IAdminTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (types, metadata) = await _service.GetAllAsync(page, pageSize, search);
        var pagination = new PaginationDto(metadata.ItemsCount, metadata.PageSize, metadata.CurrentPage);
        return Ok(new { data = types, pagination });
    }
    
    [HttpGet("all")]
    public async Task<IActionResult> GetAllNoPagination()
    {
        var types = await _service.GetAllAsync();
        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var request = new GetAdminTypeByIdRequest { AdminTypeId = id };
        var type = await _service.GetByIdAsync(request);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminTypeDto dto)
    {
        if (dto == null) return BadRequest();
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminTypeDto updateData)
    {
        var request = new UpdateAdminTypeByIdRequest { AdminTypeId = id, UpdateData = updateData };
        var updated = await _service.UpdateAsync(request);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var request = new DeleteAdminTypeRequest { AdminTypeId = id };
        await _service.DeleteAsync(request);
        return NoContent();
    }
}

