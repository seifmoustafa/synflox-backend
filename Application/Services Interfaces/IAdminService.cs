using Application.DTOs.Admin;
using Domain.Entities.Common;

namespace Application.Services;

public interface IAdminService
{
    Task<(IEnumerable<AdminDto> Admins, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<AdminDto?> GetByIdAsync(GetAdminByIdRequest request);
    Task<AdminDto?> GetByIdAsync(Guid id); // Direct overload for internal calls (from JWT)
    Task<AdminDto> CreateAsync(CreateAdminDto dto);
    Task<AdminDto?> UpdateAsync(UpdateAdminByIdRequest request);
    Task<AdminDto?> UpdateAsync(Guid id, UpdateAdminRequest data); // Direct overload for internal calls (from JWT)
    Task<bool> DeleteAsync(GetAdminByIdRequest request);
    Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword);
    Task ResetPasswordAsync(ChangePasswordByIdRequest request);
    Task<bool> ActivateAsync(GetAdminByIdRequest request);
    Task<bool> DeactivateAsync(GetAdminByIdRequest request);
    Task<int> ActivateSelectedAsync(AdminIdsRequest request);
    Task<int> DeactivateSelectedAsync(AdminIdsRequest request);
    Task<int> ActivateAllAsync();
    Task<int> DeactivateAllAsync();
    Task<int> DeleteSelectedAsync(AdminIdsRequest request);
    Task<int> DeleteAllExceptAsync(Guid exceptId);
}
