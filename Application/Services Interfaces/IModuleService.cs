using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Subscriptions;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for Module management
/// Single Responsibility: Modules only
/// </summary>
public interface IModuleService
{
    Task<ModuleDto> CreateAsync(CreateModuleDto dto);
    Task<ModuleDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<ModuleDto> Modules, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<ModuleDto?> UpdateAsync(Guid id, UpdateModuleDto dto);
    Task<bool> DeleteAsync(Guid id);
}
