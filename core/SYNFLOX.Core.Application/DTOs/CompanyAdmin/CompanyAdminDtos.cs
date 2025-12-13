using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.CompanyAdmin;

#region Response DTOs

/// <summary>
/// DTO for company admin basic information.
/// </summary>
public class CompanyAdminDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsLocked { get; set; }
    public DateTime LockedUntilUtc { get; set; }
    public AdminSessionPolicy SessionPolicy { get; set; }
    public int SessionTimeoutMinutes { get; set; }
    public DateTime LastLoginAtUtc { get; set; }
    public int TotalLogins { get; set; }
    public bool HasActiveSession { get; set; }
    public DateTime CreatedTimestamp { get; set; }
}

/// <summary>
/// DTO for company admin details including permissions.
/// </summary>
public class CompanyAdminDetailsDto : CompanyAdminDto
{
    // Permissions
    public bool CanManageDevices { get; set; }
    public bool CanViewSubscriptions { get; set; }
    public bool CanApproveReplacements { get; set; }
    public bool CanGenerateLicenses { get; set; }
    public bool CanViewUsageReports { get; set; }
    public bool CanModifySessionSettings { get; set; }
    
    // Session Configuration
    public bool AutoLogoutOnInactivity { get; set; }
    public int InactivityTimeoutMinutes { get; set; }
    public int MaxFailedAttempts { get; set; }
    public int LockoutDurationMinutes { get; set; }
    public int PasswordExpiryDays { get; set; }
    
    // Current Session Info
    public string CurrentDeviceName { get; set; } = string.Empty;
    public string CurrentSessionIp { get; set; } = string.Empty;
    public DateTime SessionStartedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
}

/// <summary>
/// DTO for admin session information.
/// </summary>
public class CompanyAdminSessionDto
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string EndReason { get; set; } = string.Empty;
    public int ActionCount { get; set; }
    public double DurationMinutes { get; set; }
}

#endregion

#region Request DTOs

/// <summary>
/// DTO for creating a new company admin account.
/// </summary>
public class CreateCompanyAdminRequest
{
    [Required]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Username { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; set; }
    
    [Required]
    [StringLength(150)]
    public required string DisplayName { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public required string Email { get; set; }
    
    [Required]
    [StringLength(50)]
    public required string Phone { get; set; }
    
    // Permissions
    public bool CanManageDevices { get; set; } = true;
    public bool CanViewSubscriptions { get; set; } = true;
    public bool CanApproveReplacements { get; set; } = true;
    public bool CanGenerateLicenses { get; set; } = true;
    public bool CanViewUsageReports { get; set; } = true;
}

/// <summary>
/// DTO for updating company admin account.
/// </summary>
public class UpdateCompanyAdminRequest
{
    [Required]
    [StringLength(150)]
    public required string DisplayName { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public required string Email { get; set; }
    
    [Required]
    [StringLength(50)]
    public required string Phone { get; set; }
    
    [Required]
    public bool IsActive { get; set; }
    
    // Permissions
    [Required]
    public bool CanManageDevices { get; set; }
    [Required]
    public bool CanViewSubscriptions { get; set; }
    [Required]
    public bool CanApproveReplacements { get; set; }
    [Required]
    public bool CanGenerateLicenses { get; set; }
    [Required]
    public bool CanViewUsageReports { get; set; }
    [Required]
    public bool CanModifySessionSettings { get; set; }
    
    // Session Configuration
    [Required]
    public AdminSessionPolicy SessionPolicy { get; set; }
    [Required]
    [Range(15, 1440)]
    public int SessionTimeoutMinutes { get; set; }
    [Required]
    public bool AutoLogoutOnInactivity { get; set; }
    [Required]
    [Range(5, 480)]
    public int InactivityTimeoutMinutes { get; set; }
    [Required]
    [Range(0, 20)]
    public int MaxFailedAttempts { get; set; }
    [Required]
    [Range(5, 1440)]
    public int LockoutDurationMinutes { get; set; }
    [Required]
    [Range(0, 365)]
    public int PasswordExpiryDays { get; set; }
}

/// <summary>
/// DTO for resetting admin password (by SYNFLOX admin).
/// </summary>
public class ResetAdminPasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; set; }
    
    public bool MustChangeOnFirstLogin { get; set; } = true;
}

/// <summary>
/// DTO for admin login.
/// </summary>
public class AdminLoginRequest
{
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string Password { get; set; }
    
    /// <summary>
    /// Device information for session tracking.
    /// </summary>
    [Required]
    public required string DeviceName { get; set; }
    [Required]
    public required string OperatingSystem { get; set; }
    [Required]
    public required string DeviceHash { get; set; }
}

/// <summary>
/// DTO for admin login response.
/// </summary>
public class AdminLoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public bool HasOtherActiveSessions { get; set; }
    public int OtherActiveSessionCount { get; set; }
    public CompanyAdminDto Admin { get; set; } = new();
}

/// <summary>
/// DTO for changing admin password (by the company admin themselves).
/// </summary>
public class CompanyAdminChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; set; }
    
    [Required]
    [Compare(nameof(NewPassword))]
    public required string ConfirmPassword { get; set; }
}

/// <summary>
/// DTO for updating admin session settings.
/// </summary>
public class UpdateSessionSettingsRequest
{
    [Required]
    public AdminSessionPolicy SessionPolicy { get; set; }
    
    [Required]
    [Range(15, 1440)]
    public int SessionTimeoutMinutes { get; set; }
    
    [Required]
    public bool AutoLogoutOnInactivity { get; set; }
    
    [Required]
    [Range(5, 480)]
    public int InactivityTimeoutMinutes { get; set; }
}

#endregion

#region ID Request DTOs (for AutoMapper decryption)

/// <summary>
/// Request DTO for getting company admin by ID.
/// </summary>
public class GetCompanyAdminByIdRequest
{
    public Guid AdminId { get; set; }
}

/// <summary>
/// Request DTO for getting company admin by company ID.
/// </summary>
public class GetCompanyAdminByCompanyIdRequest
{
    public Guid CompanyId { get; set; }
}

/// <summary>
/// Request DTO for deleting company admin.
/// </summary>
public class DeleteCompanyAdminRequest
{
    public Guid AdminId { get; set; }
}

/// <summary>
/// Request DTO for updating company admin by ID.
/// </summary>
public class UpdateCompanyAdminByIdRequest
{
    public Guid AdminId { get; set; }
}

/// <summary>
/// Request DTO for resetting password by ID.
/// </summary>
public class ResetPasswordByIdRequest
{
    public Guid AdminId { get; set; }
}

/// <summary>
/// Request DTO for unlocking account by ID.
/// </summary>
public class UnlockAccountByIdRequest
{
    public Guid AdminId { get; set; }
}

/// <summary>
/// Request DTO for terminating sessions by ID.
/// </summary>
public class TerminateSessionsByIdRequest
{
    public Guid AdminId { get; set; }
}

/// <summary>
/// Request DTO for checking if company has admin.
/// </summary>
public class CompanyHasAdminRequest
{
    public Guid CompanyId { get; set; }
}

#endregion
