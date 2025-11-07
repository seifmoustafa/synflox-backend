using Application.DTOs.Admin;
using Domain.Entities.Common;

namespace Application.Services;

public interface IAdminService
{
    Task<(IEnumerable<AdminDto> Admins, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<AdminDto?> GetByIdAsync(Guid id);
    Task<AdminDto> CreateAsync(CreateAdminDto dto);
    Task<AdminDto?> UpdateAsync(Guid id, UpdateAdminRequest dto);
    Task<bool> DeleteAsync(Guid id);
    Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword);
    Task ResetPasswordAsync(Guid id, string newPassword);
    Task<bool> ActivateAsync(Guid id);
    Task<bool> DeactivateAsync(Guid id);
    Task<int> ActivateSelectedAsync(IEnumerable<Guid> ids);
    Task<int> DeactivateSelectedAsync(IEnumerable<Guid> ids);
    Task<int> ActivateAllAsync();
    Task<int> DeactivateAllAsync();
    Task<int> DeleteSelectedAsync(IEnumerable<Guid> ids);
    Task<int> DeleteAllExceptAsync(Guid exceptId);
}
