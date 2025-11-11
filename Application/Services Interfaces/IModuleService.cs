using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Module;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for managing modules/features (e.g., Inventory, Sales, HR).
/// </summary>
public interface IModuleService
{
    /// <summary>
    /// Creates a new module.
    /// </summary>
    Task<ModuleDto> CreateModuleAsync(CreateModuleDto dto);

    /// <summary>
    /// Updates an existing module.
    /// </summary>
    Task<ModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleDto dto);

    /// <summary>
    /// Gets a module by ID.
    /// </summary>
    Task<ModuleDto?> GetModuleByIdAsync(Guid id);

    /// <summary>
    /// Gets all modules with pagination.
    /// </summary>
    Task<(IEnumerable<ModuleDto> Modules, PaginationMetadata Meta)> GetAllModulesAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null);

    /// <summary>
    /// Deletes a module (soft delete).
    /// </summary>
    Task<bool> DeleteModuleAsync(Guid id);
}

