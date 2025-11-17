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
    
    // ===== Profile Management =====
    Task<ProfileDto?> GetProfileAsync(Guid adminId);
    Task<ProfileStatisticsDto> GetProfileStatisticsAsync(Guid adminId);
    Task<ProfileDto?> UpdateProfileAsync(Guid adminId, UpdateProfileRequest request);
    Task<ProfileDto?> UpdatePreferencesAsync(Guid adminId, UpdatePreferencesRequest request);
    Task<ProfileDto?> UpdateNotificationPreferencesAsync(Guid adminId, UpdateNotificationPreferencesRequest request);
    Task<ProfileDto?> UploadProfilePictureAsync(Guid adminId, UploadProfilePictureRequest request);
    Task<ProfileDto?> DeleteProfilePictureAsync(Guid adminId);
    
    // ===== Password & Security =====
    Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword);
    Task ResetPasswordAsync(ChangePasswordByIdRequest request);
    
    // ===== Two-Factor Authentication =====
    Task<TwoFactorSetupDto> Generate2FASecretAsync(Guid adminId);
    Task<bool> Verify2FAAsync(Guid adminId, string verificationCode);
    Task Disable2FAAsync(Guid adminId);
    
    // ===== Bulk Operations =====
    Task<bool> ActivateAsync(GetAdminByIdRequest request);
    Task<bool> DeactivateAsync(GetAdminByIdRequest request);
    Task<int> ActivateSelectedAsync(AdminIdsRequest request);
    Task<int> DeactivateSelectedAsync(AdminIdsRequest request);
    Task<int> ActivateAllAsync();
    Task<int> DeactivateAllAsync();
    Task<int> DeleteSelectedAsync(AdminIdsRequest request);
    Task<int> DeleteAllExceptAsync(Guid exceptId);
}
