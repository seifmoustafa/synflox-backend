using Application.DTOs.Admin;
using Application.DTOs.Authentication;
using System;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service for managing admin profile operations (current user only)
/// Handles profile updates, preferences, 2FA, password changes, and security settings
/// Separated from IAdminService for Single Responsibility Principle
/// </summary>
public interface IAdminProfileService
{
    // ===== Profile Information =====
    
    /// <summary>
    /// Get current admin's profile with full information
    /// </summary>
    Task<ProfileDto?> GetMyProfileAsync(Guid currentUserId);
    
    /// <summary>
    /// Get profile statistics for current admin (logins, managed resources, etc.)
    /// </summary>
    Task<ProfileStatisticsDto> GetMyStatisticsAsync(Guid currentUserId);
    
    // ===== Profile Updates =====
    
    /// <summary>
    /// Update current admin's basic profile information (name, email, phone, etc.)
    /// </summary>
    Task<ProfileDto?> UpdateMyProfileAsync(Guid currentUserId, UpdateProfileRequest request);
    
    /// <summary>
    /// Update current admin's preferences (language, theme, timezone)
    /// </summary>
    Task<ProfileDto?> UpdateMyPreferencesAsync(Guid currentUserId, UpdatePreferencesRequest request);
    
    /// <summary>
    /// Update current admin's notification preferences
    /// </summary>
    Task<ProfileDto?> UpdateMyNotificationPreferencesAsync(Guid currentUserId, UpdateNotificationPreferencesRequest request);
    
    // ===== Profile Picture =====
    
    /// <summary>
    /// Upload profile picture for current admin
    /// </summary>
    Task<ProfileDto?> UploadMyProfilePictureAsync(Guid currentUserId, UploadProfilePictureRequest request);
    
    /// <summary>
    /// Delete profile picture for current admin
    /// </summary>
    Task<ProfileDto?> DeleteMyProfilePictureAsync(Guid currentUserId);
    
    // ===== Password Management =====
    
    /// <summary>
    /// Change current admin's password (requires current password verification)
    /// </summary>
    Task ChangeMyPasswordAsync(Guid currentUserId, ChangePasswordRequest request);
    
    /// <summary>
    /// Change current admin's password with 2FA verification
    /// Requires either TwoFactorCode or BackupCode if 2FA is enabled
    /// Invalidates all refresh tokens after password change
    /// </summary>
    Task ChangeMyPasswordWith2FAAsync(Guid currentUserId, ChangePasswordWith2FARequest request);
    
    // ===== Two-Factor Authentication =====
    
    /// <summary>
    /// Generate 2FA secret and QR code for current admin
    /// </summary>
    Task<TwoFactorSetupDto> Enable2FAAsync(Guid currentUserId);
    
    /// <summary>
    /// Verify 2FA code and enable 2FA for current admin
    /// </summary>
    Task<bool> Verify2FAAsync(Guid currentUserId, string verificationCode);
    
    /// <summary>
    /// Disable 2FA for current admin
    /// Requires password confirmation for security
    /// Deletes all backup codes automatically
    /// </summary>
    Task Disable2FAAsync(Guid currentUserId, string currentPassword);
    
    /// <summary>
    /// Reset 2FA for current admin (generates new secret)
    /// Requires password confirmation for security
    /// Deletes all old backup codes
    /// Used when user loses access to authenticator app but still has account access
    /// </summary>
    Task<TwoFactorSetupDto> Reset2FAAsync(Guid currentUserId, string currentPassword);
    
    // ===== Account Management =====
    
    /// <summary>
    /// Delete current admin's account (soft delete)
    /// </summary>
    Task<bool> DeleteMyAccountAsync(Guid currentUserId);
}
