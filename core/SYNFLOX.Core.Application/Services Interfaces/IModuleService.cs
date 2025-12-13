using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.Common;
using Application.DTOs.ModuleDto;
using Domain.Entities.Common;

namespace Application.Services;

/// <summary>
/// Service interface for Module management
/// Single Responsibility: Modules only
/// SYNFLOX Rule: All ID operations use DTOs with AutoMapper decryption
/// </summary>
public interface IModuleService
{
    Task<ModuleDto> CreateAsync(CreateModuleDto dto);
    Task<ModuleDto?> GetByIdAsync(ModuleIdRequest request);
    Task<(IEnumerable<ModuleDto> Modules, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<ModuleDto?> UpdateAsync(ModuleIdRequest request, UpdateModuleDto dto);
    
    /// <summary>
    /// Preview what will be affected by deleting a module
    /// </summary>
    Task<DeletePreviewDto> GetDeletePreviewAsync(ModuleIdRequest request);
    
    /// <summary>
    /// Delete a module with optional cascade
    /// </summary>
    Task<bool> DeleteAsync(ModuleIdRequest request, bool confirmCascade = false);
    
    // Activate/Deactivate operations
    Task ActivateAsync(ModuleIdRequest request);
    Task DeactivateAsync(ModuleIdRequest request);
}
