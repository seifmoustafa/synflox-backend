using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service interface for admin CRUD operations (SuperAdmin only)
/// Profile operations moved to IAdminProfileService for Single Responsibility
/// </summary>
public interface IAdminService
{
    // ===== Read Operations =====
    Task<(IEnumerable<AdminDto> Admins, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<AdminDto?> GetByIdAsync(GetAdminByIdRequest request);
    Task<AdminDto?> GetByIdAsync(Guid id); // Direct overload for internal calls (from JWT)
    
    // ===== Create Operation =====
    Task<AdminDto> CreateAsync(CreateAdminDto dto);
    
    // ===== Update Operations =====
    Task<AdminDto?> UpdateAsync(UpdateAdminByIdRequest request);
    Task<AdminDto?> UpdateAsync(Guid id, UpdateAdminRequest data); // Direct overload for internal calls
    
    // ===== Password Operations =====
    Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword);
    Task ResetPasswordAsync(ChangePasswordByIdRequest request);
    
    // ===== Activate/Deactivate Operations =====
    Task<bool> ActivateAsync(GetAdminByIdRequest request);
    Task<bool> DeactivateAsync(GetAdminByIdRequest request);
    Task<int> ActivateSelectedAsync(AdminIdsRequest request);
    Task<int> DeactivateSelectedAsync(AdminIdsRequest request);
    Task<int> ActivateAllAsync();
    Task<int> DeactivateAllAsync();
    
    // ===== Delete Operations =====
    Task<bool> DeleteAsync(GetAdminByIdRequest request);
    Task<int> DeleteSelectedAsync(AdminIdsRequest request);
    Task<int> DeleteAllExceptAsync(Guid exceptId);
}
