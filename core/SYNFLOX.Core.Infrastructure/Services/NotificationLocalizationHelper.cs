using Application.Services;
using Domain.Enums;
using System.Globalization;

namespace Infrastructure.Services;

/// <summary>
/// Helper class for managing localized notification content.
/// Follows the same pattern as EmailLocalizationHelper for consistency.
/// </summary>
public class NotificationLocalizationHelper
{
    private readonly ILocalizationService _localizer;

    public NotificationLocalizationHelper(ILocalizationService localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// Set the culture for localization based on language parameter.
    /// If no language is specified, uses the current culture (from Accept-Language header)
    /// </summary>
    public void SetCulture(string? language)
    {
        if (string.IsNullOrEmpty(language))
        {
            var currentLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            language = currentLang == "ar" ? "ar" : "en";
        }

        var culture = language.ToLower() switch
        {
            "ar" => new CultureInfo("ar"),
            "en" => new CultureInfo("en"),
            _ => new CultureInfo("en")
        };

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>
    /// Check if current culture is RTL (Arabic)
    /// </summary>
    public bool IsRtl() => CultureInfo.CurrentCulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    #region Subscription Notifications

    public NotificationLocalizedContent GetSubscriptionExpiring30Days(string planName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Expiring30Days.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Expiring30Days.Message"], planName),
            Icon = "calendar-warning",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetSubscriptionExpiring7Days(string planName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Expiring7Days.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Expiring7Days.Message"], planName),
            Icon = "alert-triangle",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.High
        };
    }

    public NotificationLocalizedContent GetSubscriptionExpiring1Day(string planName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Expiring1Day.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Expiring1Day.Message"], planName),
            Icon = "alert-circle",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Urgent
        };
    }

    public NotificationLocalizedContent GetSubscriptionExpired(string planName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Expired.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Expired.Message"], planName),
            Icon = "x-circle",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Urgent
        };
    }

    public NotificationLocalizedContent GetSubscriptionRenewed(string planName, int daysRemaining)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Renewed.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Renewed.Message"], planName, daysRemaining),
            Icon = "check-circle",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetSubscriptionActivated(string planName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Activated.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Activated.Message"], planName),
            Icon = "rocket",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetSubscriptionSuspended(string planName, string reason)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Suspended.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Suspended.Message"], planName, reason),
            Icon = "pause-circle",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Urgent
        };
    }

    public NotificationLocalizedContent GetSubscriptionUpgraded(string oldPlan, string newPlan)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.Upgraded.Title"],
            Message = string.Format(_localizer["Notification.Subscription.Upgraded.Message"], oldPlan, newPlan),
            Icon = "arrow-up-circle",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetTrialStarted(string planName, int trialDays)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.TrialStarted.Title"],
            Message = string.Format(_localizer["Notification.Subscription.TrialStarted.Message"], planName, trialDays),
            Icon = "gift",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetTrialEnding(string planName, int daysRemaining)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Subscription.TrialEnding.Title"],
            Message = string.Format(_localizer["Notification.Subscription.TrialEnding.Message"], planName, daysRemaining),
            Icon = "clock",
            ActionUrl = "/subscription",
            Priority = NotificationPriority.High
        };
    }

    #endregion

    #region Device Notifications

    public NotificationLocalizedContent GetDeviceLimitReached(int maxDevices)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Device.LimitReached.Title"],
            Message = string.Format(_localizer["Notification.Device.LimitReached.Message"], maxDevices),
            Icon = "monitor-x",
            ActionUrl = "/devices",
            Priority = NotificationPriority.High
        };
    }

    public NotificationLocalizedContent GetNewDeviceActivated(string deviceName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Device.Activated.Title"],
            Message = string.Format(_localizer["Notification.Device.Activated.Message"], deviceName),
            Icon = "monitor-check",
            ActionUrl = "/devices",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetDeviceDeactivated(string deviceName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Device.Deactivated.Title"],
            Message = string.Format(_localizer["Notification.Device.Deactivated.Message"], deviceName),
            Icon = "monitor-off",
            ActionUrl = "/devices",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetDeviceBlocked(string deviceName, string reason)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Device.Blocked.Title"],
            Message = string.Format(_localizer["Notification.Device.Blocked.Message"], deviceName, reason),
            Icon = "shield-x",
            ActionUrl = "/devices",
            Priority = NotificationPriority.High
        };
    }

    public NotificationLocalizedContent GetOfflineLicenseCreated(string deviceName, int offlineDays)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Device.OfflineLicense.Title"],
            Message = string.Format(_localizer["Notification.Device.OfflineLicense.Message"], deviceName, offlineDays),
            Icon = "wifi-off",
            ActionUrl = "/devices",
            Priority = NotificationPriority.Normal
        };
    }

    #endregion

    #region Security Notifications

    public NotificationLocalizedContent GetNewLoginDetected(string location, string device)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Security.NewLogin.Title"],
            Message = string.Format(_localizer["Notification.Security.NewLogin.Message"], location, device),
            Icon = "log-in",
            ActionUrl = "/security/sessions",
            Priority = NotificationPriority.High
        };
    }

    public NotificationLocalizedContent GetPasswordChanged()
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Security.PasswordChanged.Title"],
            Message = _localizer["Notification.Security.PasswordChanged.Message"],
            Icon = "key",
            ActionUrl = "/security",
            Priority = NotificationPriority.High
        };
    }

    public NotificationLocalizedContent GetEmailChanged(string newEmail)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Security.EmailChanged.Title"],
            Message = string.Format(_localizer["Notification.Security.EmailChanged.Message"], newEmail),
            Icon = "mail",
            ActionUrl = "/security",
            Priority = NotificationPriority.High
        };
    }

    public NotificationLocalizedContent Get2FAEnabled()
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Security.2FAEnabled.Title"],
            Message = _localizer["Notification.Security.2FAEnabled.Message"],
            Icon = "shield-check",
            ActionUrl = "/security/2fa",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent Get2FADisabled()
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Security.2FADisabled.Title"],
            Message = _localizer["Notification.Security.2FADisabled.Message"],
            Icon = "shield-off",
            ActionUrl = "/security/2fa",
            Priority = NotificationPriority.High
        };
    }

    public NotificationLocalizedContent GetSuspiciousActivity(string activityType)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Security.Suspicious.Title"],
            Message = string.Format(_localizer["Notification.Security.Suspicious.Message"], activityType),
            Icon = "alert-octagon",
            ActionUrl = "/security",
            Priority = NotificationPriority.Urgent
        };
    }

    public NotificationLocalizedContent GetBackupCodesGenerated(int codesCount)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Security.BackupCodes.Title"],
            Message = string.Format(_localizer["Notification.Security.BackupCodes.Message"], codesCount),
            Icon = "file-key",
            ActionUrl = "/security/2fa",
            Priority = NotificationPriority.Normal
        };
    }

    #endregion

    #region System Notifications

    public NotificationLocalizedContent GetMaintenanceScheduled(DateTime maintenanceTime)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.System.Maintenance.Title"],
            Message = string.Format(_localizer["Notification.System.Maintenance.Message"], maintenanceTime.ToString("g")),
            Icon = "tool",
            ActionUrl = null,
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetSystemUpdate(string version)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.System.Update.Title"],
            Message = string.Format(_localizer["Notification.System.Update.Message"], version),
            Icon = "download-cloud",
            ActionUrl = "/changelog",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetAnnouncement(string title, string message)
    {
        return new NotificationLocalizedContent
        {
            Title = title,
            Message = message,
            Icon = "megaphone",
            ActionUrl = null,
            Priority = NotificationPriority.Normal
        };
    }

    #endregion

    #region Company Notifications (for admin)

    public NotificationLocalizedContent GetNewCompanyRegistered(string companyName)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Company.Registered.Title"],
            Message = string.Format(_localizer["Notification.Company.Registered.Message"], companyName),
            Icon = "building",
            ActionUrl = "/companies",
            Priority = NotificationPriority.Normal
        };
    }

    public NotificationLocalizedContent GetCompanySubscriptionExpiring(string companyName, int daysRemaining)
    {
        return new NotificationLocalizedContent
        {
            Title = _localizer["Notification.Company.Expiring.Title"],
            Message = string.Format(_localizer["Notification.Company.Expiring.Message"], companyName, daysRemaining),
            Icon = "building-2",
            ActionUrl = "/companies",
            Priority = daysRemaining <= 7 ? NotificationPriority.High : NotificationPriority.Normal
        };
    }

    #endregion
}

/// <summary>
/// Represents localized content for a notification
/// </summary>
public class NotificationLocalizedContent
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = "bell";
    public string? ActionUrl { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}
