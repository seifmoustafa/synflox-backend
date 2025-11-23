using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    Task<bool> DeleteAsync(ModuleIdRequest request);
    
    // Activate/Deactivate operations
    Task ActivateAsync(ModuleIdRequest request);
    Task DeactivateAsync(ModuleIdRequest request);
}
