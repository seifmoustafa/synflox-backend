using Domain.Enums;

namespace Application.DTOs.Admin;

/// <summary>
/// Complete profile information for an admin user
/// </summary>
public class ProfileDto
{
    // Basic Information
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Bio { get; set; }

    // Profile Picture
    public string? ProfilePictureUrl { get; set; }

    // Professional Information
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }

    // Preferences
    public string? PreferredLanguage { get; set; }
    public string? Timezone { get; set; }
    public string? ThemePreference { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }

    // Social & Contact
    public string? LinkedInUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? BackupEmail { get; set; }

    // Security & Activity
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }
    public int LoginCount { get; set; }
    public bool IsTwoFactorEnabled { get; set; }

    // Notification Preferences
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool CompanyExpiryNotifications { get; set; }
    public bool SubscriptionExpiryNotifications { get; set; }
    public bool SystemAlertsNotifications { get; set; }

    // Admin Type
    public Guid AdminTypeId { get; set; }
    public string? AdminTypeName { get; set; }

    // Audit
    public DateTime CreatedTimestamp { get; set; }
    public DateTime UpdatedTimestamp { get; set; }
}

/// <summary>
/// Request to update basic profile information
/// </summary>
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Bio { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? BackupEmail { get; set; }
}

/// <summary>
/// Request to update user preferences
/// </summary>
public class UpdatePreferencesRequest
{
    public string? PreferredLanguage { get; set; }
    public string? Timezone { get; set; }
    public string? ThemePreference { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
}

/// <summary>
/// Request to update notification preferences
/// </summary>
public class UpdateNotificationPreferencesRequest
{
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool CompanyExpiryNotifications { get; set; }
    public bool SubscriptionExpiryNotifications { get; set; }
    public bool SystemAlertsNotifications { get; set; }
}

/// <summary>
/// Request to upload profile picture
/// </summary>
public class UploadProfilePictureRequest
{
    public required string Base64Image { get; set; }
}

/// <summary>
/// Request to enable 2FA
/// </summary>
public class Enable2FARequest
{
    public required string VerificationCode { get; set; }
}

/// <summary>
/// Profile statistics for dashboard
/// </summary>
public class ProfileStatisticsDto
{
    public int TotalLogins { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int DaysSinceCreation { get; set; }
    public int DaysSinceLastPasswordChange { get; set; }
    public int CompaniesManaged { get; set; }
    public int SubscriptionsManaged { get; set; }
    public int AdminsCreated { get; set; }
}

/// <summary>
/// 2FA setup information including QR code
/// </summary>
public class TwoFactorSetupDto
{
    public required string Secret { get; set; }
    public required string QRCodeBase64 { get; set; }
    public required string ManualEntryKey { get; set; }
    public required string AccountName { get; set; }
    public required string Issuer { get; set; }
}
