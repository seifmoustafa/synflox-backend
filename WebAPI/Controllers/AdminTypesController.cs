using System;
using System.Threading.Tasks;
using Application.DTOs.AdminType;
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
    private readonly IIdEncryptionService _idEncryption;

    public AdminTypesController(IAdminTypeService service, IIdEncryptionService idEncryption)
    {
        _service = service;
        _idEncryption = idEncryption;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _service.GetAllAsync();
        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var type = await _service.GetByIdAsync(_idEncryption.Decrypt(id));
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
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdminTypeDto dto)
    {
        var updated = await _service.UpdateAsync(_idEncryption.Decrypt(id), dto);
        return updated is null ? NotFound() : Ok(updated);
    }
}

