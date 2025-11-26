using Application.Services;
using System.Globalization;

namespace Infrastructure.Services;

/// <summary>
/// Helper class for managing localized email content
/// </summary>
public class EmailLocalizationHelper
{
    private readonly ILocalizationService _localizer;

    public EmailLocalizationHelper(ILocalizationService localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// Set the culture for localization based on language parameter
    /// If no language is specified, uses the current culture (from Accept-Language header)
    /// </summary>
    /// <param name="language">Language code (en, ar) - if null, uses current culture</param>
    public void SetCulture(string? language)
    {
        // If no language specified, use current culture set by middleware from Accept-Language header
        if (string.IsNullOrEmpty(language))
        {
            var currentLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            language = currentLang == "ar" ? "ar" : "en"; // Respect current culture, default to English
        }

        var culture = language.ToLower() switch
        {
            "ar" => new CultureInfo("ar"),
            "en" => new CultureInfo("en"),
            _ => new CultureInfo("en") // Default fallback
        };

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>
    /// Get localized email content for company operations
    /// </summary>
    public EmailLocalizedContent GetCompanyCreatedContent()
    {
        return new EmailLocalizedContent
        {
            Subject = _localizer["Email.Company.Created.Subject"],
            Title = _localizer["Email.Company.Created.Title"],
            WelcomeMessage = _localizer["Email.Company.Created.WelcomeMessage"],
            CompanyNameLabel = _localizer["Email.Company.CompanyName"],
            RegistrationDateLabel = _localizer["Email.Company.RegistrationDate"],
            ContactEmailLabel = _localizer["Email.Company.ContactEmail"],
            WhatsNextTitle = _localizer["Email.Company.WhatsNext"],
            WhatsNextItems = new[]
            {
                _localizer["Email.Company.WhatsNext.SetupPlan"],
                _localizer["Email.Company.WhatsNext.ConfigureLicensing"],
                _localizer["Email.Company.WhatsNext.ExploreFeatures"],
                _localizer["Email.Company.WhatsNext.ContactSupport"],
                _localizer["Email.Company.WhatsNext.AccessDashboard"]
            },
            FeaturesTitle = _localizer["Email.Company.Features"],
            FeaturesItems = new[]
            {
                _localizer["Email.Company.Features.LicenseManagement"],
                _localizer["Email.Company.Features.Analytics"],
                _localizer["Email.Company.Features.CloudInfrastructure"],
                _localizer["Email.Company.Features.MultiPlan"],
                _localizer["Email.Company.Features.Support"]
            },
            ThankYouMessage = _localizer["Email.Company.ThankYou"]
        };
    }

    public EmailLocalizedContent GetCompanyUpdatedContent()
    {
        return new EmailLocalizedContent
        {
            Subject = _localizer["Email.Company.Updated.Subject"],
            Title = _localizer["Email.Company.Updated.Title"],
            WelcomeMessage = _localizer["Email.Company.Updated.Message"],
            CompanyNameLabel = _localizer["Email.Company.CompanyName"],
            UpdatedFieldsLabel = _localizer["Email.Company.UpdatedFields"],
            UpdateDateLabel = _localizer["Email.Company.UpdateDate"]
        };
    }

    public EmailLocalizedContent GetCompanyActivatedContent()
    {
        return new EmailLocalizedContent
        {
            Subject = _localizer["Email.Company.Activated.Subject"],
            Title = _localizer["Email.Company.Activated.Title"],
            WelcomeMessage = _localizer["Email.Company.Activated.Message"],
            CompanyNameLabel = _localizer["Email.Company.CompanyName"],
            ActivationDateLabel = _localizer["Email.Company.ActivationDate"]
        };
    }

    public EmailLocalizedContent GetCompanyDeactivatedContent()
    {
        return new EmailLocalizedContent
        {
            Subject = _localizer["Email.Company.Deactivated.Subject"],
            Title = _localizer["Email.Company.Deactivated.Title"],
            WelcomeMessage = _localizer["Email.Company.Deactivated.Message"],
            CompanyNameLabel = _localizer["Email.Company.CompanyName"],
            ReasonLabel = _localizer["Email.Company.Reason"],
            DeactivationDateLabel = _localizer["Email.Company.DeactivationDate"]
        };
    }

    public EmailLocalizedContent GetCompanyDeletedContent()
    {
        return new EmailLocalizedContent
        {
            Subject = _localizer["Email.Company.Deleted.Subject"],
            Title = _localizer["Email.Company.Deleted.Title"],
            WelcomeMessage = _localizer["Email.Company.Deleted.Message"],
            CompanyNameLabel = _localizer["Email.Company.CompanyName"],
            DeletionDateLabel = _localizer["Email.Company.DeletionDate"]
        };
    }

    public EmailLocalizedContent GetCustomEmailContent()
    {
        return new EmailLocalizedContent
        {
            CompanyNameLabel = _localizer["Email.Company.CompanyName"],
            DateLabel = _localizer["Email.Common.Date"],
            MessageLabel = _localizer["Email.Common.Message"],
            ReasonLabel = _localizer["Email.Company.Reason"],
            NotesLabel = _localizer["Email.Common.Notes"]
        };
    }

    /// <summary>
    /// Get common email elements (footer, support info, etc.)
    /// </summary>
    public EmailCommonContent GetCommonContent()
    {
        return new EmailCommonContent
        {
            NotificationDetailsTitle = _localizer["Email.Common.NotificationDetails"],
            CompanyLabel = _localizer["Email.Common.Company"],
            NotificationTimeLabel = _localizer["Email.Common.NotificationTime"],
            SystemLabel = _localizer["Email.Common.System"],
            NeedAssistanceTitle = _localizer["Email.Common.NeedAssistance"],
            NeedAssistanceMessage = _localizer["Email.Common.NeedAssistanceMessage"],
            EmailLabel = _localizer["Email.Common.Email"],
            WebsiteLabel = _localizer["Email.Common.Website"],
            AutomatedMessage = _localizer["Email.Common.AutomatedMessage"],
            FooterText = _localizer["Email.Common.FooterText"],
            CopyrightText = _localizer["Email.Common.Copyright"],
            HelloLabel = _localizer["Email.Common.Hello"],
            NeedHelpLabel = _localizer["Email.Common.NeedHelp"],
            SupportMessage = _localizer["Email.Common.SupportMessage"],
            PoweredByText = _localizer["Email.Common.PoweredBy"]
        };
    }

    /// <summary>
    /// Check if current culture is RTL (Arabic)
    /// </summary>
    public bool IsRtl()
    {
        return CultureInfo.CurrentCulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get text direction based on culture
    /// </summary>
    public string GetTextDirection() => IsRtl() ? "rtl" : "ltr";

    /// <summary>
    /// Get text alignment based on culture
    /// </summary>
    public string GetTextAlign() => IsRtl() ? "right" : "left";

    // ===== SUBSCRIPTION ACTION EMAIL CONTENT METHODS =====

    public SubscriptionActionEmailContent GetSubscriptionSuspendedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Suspended.Subject"],
            Title = _localizer["Email.Subscription.Suspended.Title"],
            Description = _localizer["Email.Subscription.Suspended.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.SuspendedDateLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Suspended.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Suspended.Means.AccessDisabled"],
                _localizer["Email.Subscription.Suspended.Means.DataSafe"],
                _localizer["Email.Subscription.Suspended.Means.CanResume"],
                _localizer["Email.Subscription.Suspended.Means.ContactSupport"]
            },
            NextStepsTitle = _localizer["Email.Subscription.NextSteps"],
            NextStepsItems = new[]
            {
                _localizer["Email.Subscription.Suspended.NextSteps.ReviewReason"],
                _localizer["Email.Subscription.Suspended.NextSteps.ContactSupport"],
                _localizer["Email.Subscription.Suspended.NextSteps.ResolveIssue"]
            },
            ClosingMessage = _localizer["Email.Subscription.Suspended.Closing"],
            ActionSignature = _localizer["Email.Subscription.Suspended.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionResumedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Resumed.Subject"],
            Title = _localizer["Email.Subscription.Resumed.Title"],
            Description = _localizer["Email.Subscription.Resumed.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.ResumedDateLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.ExpiryDateLabel"],
            DaysRemainingLabel = _localizer["Email.Subscription.DaysRemainingLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Resumed.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Resumed.Means.AccessRestored"],
                _localizer["Email.Subscription.Resumed.Means.AllFeatures"],
                _localizer["Email.Subscription.Resumed.Means.Support"],
                _localizer["Email.Subscription.Resumed.Means.DataIntact"]
            },
            ClosingMessage = _localizer["Email.Subscription.Resumed.Closing"],
            ActionSignature = _localizer["Email.Subscription.Resumed.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionCanceledContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Canceled.Subject"],
            Title = _localizer["Email.Subscription.Canceled.Title"],
            Description = _localizer["Email.Subscription.Canceled.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.CanceledDateLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Canceled.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Canceled.Means.Terminated"],
                _localizer["Email.Subscription.Canceled.Means.AccessRevoked"],
                _localizer["Email.Subscription.Canceled.Means.DataRetention"],
                _localizer["Email.Subscription.Canceled.Means.CanReactivate"]
            },
            NextStepsTitle = _localizer["Email.Subscription.Canceled.WantToReturn"],
            NextStepsItems = new[]
            {
                _localizer["Email.Subscription.Canceled.Return.ContactSupport"],
                _localizer["Email.Subscription.Canceled.Return.ChoosePlan"],
                _localizer["Email.Subscription.Canceled.Return.RestoreData"]
            },
            ClosingMessage = _localizer["Email.Subscription.Canceled.Closing"],
            ActionSignature = _localizer["Email.Subscription.Canceled.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionPausedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Paused.Subject"],
            Title = _localizer["Email.Subscription.Paused.Title"],
            Description = _localizer["Email.Subscription.Paused.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.PausedDateLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Paused.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Paused.Means.TimerPaused"],
                _localizer["Email.Subscription.Paused.Means.NoBilling"],
                _localizer["Email.Subscription.Paused.Means.DataSafe"],
                _localizer["Email.Subscription.Paused.Means.CanResume"]
            },
            ClosingMessage = _localizer["Email.Subscription.Paused.Closing"],
            ActionSignature = _localizer["Email.Subscription.Paused.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionUnpausedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Unpaused.Subject"],
            Title = _localizer["Email.Subscription.Unpaused.Title"],
            Description = _localizer["Email.Subscription.Unpaused.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.UnpausedDateLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.ExpiryDateLabel"],
            DaysRemainingLabel = _localizer["Email.Subscription.DaysRemainingLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Unpaused.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Unpaused.Means.TimerResumed"],
                _localizer["Email.Subscription.Unpaused.Means.FullAccess"],
                _localizer["Email.Subscription.Unpaused.Means.BillingResumes"]
            },
            ClosingMessage = _localizer["Email.Subscription.Unpaused.Closing"],
            ActionSignature = _localizer["Email.Subscription.Unpaused.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionReactivatedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Reactivated.Subject"],
            Title = _localizer["Email.Subscription.Reactivated.Title"],
            Description = _localizer["Email.Subscription.Reactivated.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.ReactivatedDateLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.ExpiryDateLabel"],
            DaysRemainingLabel = _localizer["Email.Subscription.DaysRemainingLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Reactivated.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Reactivated.Means.FullAccess"],
                _localizer["Email.Subscription.Reactivated.Means.AllFeatures"],
                _localizer["Email.Subscription.Reactivated.Means.Support"],
                _localizer["Email.Subscription.Reactivated.Means.DataRestored"]
            },
            ClosingMessage = _localizer["Email.Subscription.Reactivated.Closing"],
            ActionSignature = _localizer["Email.Subscription.Reactivated.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetTrialStartedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.TrialStarted.Subject"],
            Title = _localizer["Email.Subscription.TrialStarted.Title"],
            Description = _localizer["Email.Subscription.TrialStarted.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.TrialStartedDateLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.TrialExpiresDateLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.TrialStarted.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.TrialStarted.Means.FullAccess"],
                _localizer["Email.Subscription.TrialStarted.Means.AllFeatures"],
                _localizer["Email.Subscription.TrialStarted.Means.Analytics"],
                _localizer["Email.Subscription.TrialStarted.Means.Support"],
                _localizer["Email.Subscription.TrialStarted.Means.NoLimits"],
                _localizer["Email.Subscription.TrialStarted.Means.Secure"]
            },
            NextStepsTitle = _localizer["Email.Subscription.TrialStarted.NextSteps"],
            NextStepsItems = new[]
            {
                _localizer["Email.Subscription.TrialStarted.Steps.Explore"],
                _localizer["Email.Subscription.TrialStarted.Steps.Test"],
                _localizer["Email.Subscription.TrialStarted.Steps.Support"],
                _localizer["Email.Subscription.TrialStarted.Steps.Upgrade"]
            },
            ClosingMessage = _localizer["Email.Subscription.TrialStarted.Closing"],
            ActionSignature = _localizer["Email.Subscription.TrialStarted.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetTrialStoppedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.TrialStopped.Subject"],
            Title = _localizer["Email.Subscription.TrialStopped.Title"],
            Description = _localizer["Email.Subscription.TrialStopped.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.ConversionDateLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.ExpiryDateLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.TrialStopped.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.TrialStopped.Means.Converted"],
                _localizer["Email.Subscription.TrialStopped.Means.FullAccess"],
                _localizer["Email.Subscription.TrialStopped.Means.Support"],
                _localizer["Email.Subscription.TrialStopped.Means.NoInterruption"]
            },
            ClosingMessage = _localizer["Email.Subscription.TrialStopped.Closing"],
            ActionSignature = _localizer["Email.Subscription.TrialStopped.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionRenewedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Renewed.Subject"],
            Title = _localizer["Email.Subscription.Renewed.Title"],
            Description = _localizer["Email.Subscription.Renewed.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.RenewalDateLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.NewExpiryDateLabel"],
            DaysRemainingLabel = _localizer["Email.Subscription.ExtendedDaysLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Renewed.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Renewed.Means.Continued"],
                _localizer["Email.Subscription.Renewed.Means.AllFeatures"],
                _localizer["Email.Subscription.Renewed.Means.Support"],
                _localizer["Email.Subscription.Renewed.Means.Updates"]
            },
            ClosingMessage = _localizer["Email.Subscription.Renewed.Closing"],
            ActionSignature = _localizer["Email.Subscription.Renewed.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionActivatedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Activated.Subject"],
            Title = _localizer["Email.Subscription.Activated.Title"],
            Description = _localizer["Email.Subscription.Activated.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.ActivationDateLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.ExpiryDateLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Activated.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Activated.Means.FullAccess"],
                _localizer["Email.Subscription.Activated.Means.AllFeatures"],
                _localizer["Email.Subscription.Activated.Means.Support"],
                _localizer["Email.Subscription.Activated.Means.Updates"],
                _localizer["Email.Subscription.Activated.Means.Secure"]
            },
            ClosingMessage = _localizer["Email.Subscription.Activated.Closing"],
            ActionSignature = _localizer["Email.Subscription.Activated.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionExpiredContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Expired.Subject"],
            Title = _localizer["Email.Subscription.Expired.Title"],
            Description = _localizer["Email.Subscription.Expired.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.ExpirationDateLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Expired.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Expired.Means.AccessSuspended"],
                _localizer["Email.Subscription.Expired.Means.DataSafe"],
                _localizer["Email.Subscription.Expired.Means.NeedRenew"],
                _localizer["Email.Subscription.Expired.Means.ContactSupport"]
            },
            NextStepsTitle = _localizer["Email.Subscription.Expired.NextSteps"],
            NextStepsItems = new[]
            {
                _localizer["Email.Subscription.Expired.Steps.ContactSales"],
                _localizer["Email.Subscription.Expired.Steps.ChoosePlan"],
                _localizer["Email.Subscription.Expired.Steps.RestoreAccess"]
            },
            ClosingMessage = _localizer["Email.Subscription.Expired.Closing"],
            ActionSignature = _localizer["Email.Subscription.Expired.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionUpgradedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Upgraded.Subject"],
            Title = _localizer["Email.Subscription.Upgraded.Title"],
            Description = _localizer["Email.Subscription.Upgraded.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            UpgradeLabel = _localizer["Email.Subscription.UpgradeLabel"],
            DateLabel = _localizer["Email.Subscription.UpgradeDateLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.NewExpiryDateLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Upgraded.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Upgraded.Means.Enhanced"],
                _localizer["Email.Subscription.Upgraded.Means.Improved"],
                _localizer["Email.Subscription.Upgraded.Means.Support"],
                _localizer["Email.Subscription.Upgraded.Means.Analytics"],
                _localizer["Email.Subscription.Upgraded.Means.Extended"],
                _localizer["Email.Subscription.Upgraded.Means.Secure"]
            },
            ClosingMessage = _localizer["Email.Subscription.Upgraded.Closing"],
            ActionSignature = _localizer["Email.Subscription.Upgraded.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetSubscriptionExtendedContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.Extended.Subject"],
            Title = _localizer["Email.Subscription.Extended.Title"],
            Description = _localizer["Email.Subscription.Extended.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.ExtensionDateLabel"],
            PreviousExpiryLabel = _localizer["Email.Subscription.PreviousExpiryLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.NewExpiryDateLabel"],
            ExtensionPeriodLabel = _localizer["Email.Subscription.ExtensionPeriodLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            ReasonLabel = _localizer["Email.Subscription.ReasonLabel"],
            NotesLabel = _localizer["Email.Subscription.NotesLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.Extended.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.Extended.Means.Continued"],
                _localizer["Email.Subscription.Extended.Means.Extended"],
                _localizer["Email.Subscription.Extended.Means.Support"],
                _localizer["Email.Subscription.Extended.Means.Updates"],
                _localizer["Email.Subscription.Extended.Means.Secure"]
            },
            ClosingMessage = _localizer["Email.Subscription.Extended.Closing"],
            ActionSignature = _localizer["Email.Subscription.Extended.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetTrialExpiringContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.TrialExpiring.Subject"],
            Title = _localizer["Email.Subscription.TrialExpiring.Title"],
            Description = _localizer["Email.Subscription.TrialExpiring.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.ExpirationDateLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.TrialExpiring.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.TrialExpiring.Means.Premium"],
                _localizer["Email.Subscription.TrialExpiring.Means.Management"],
                _localizer["Email.Subscription.TrialExpiring.Means.Analytics"],
                _localizer["Email.Subscription.TrialExpiring.Means.Support"],
                _localizer["Email.Subscription.TrialExpiring.Means.Secure"]
            },
            NextStepsTitle = _localizer["Email.Subscription.TrialExpiring.NextSteps"],
            NextStepsItems = new[]
            {
                _localizer["Email.Subscription.TrialExpiring.Steps.Uninterrupted"],
                _localizer["Email.Subscription.TrialExpiring.Steps.AllFeatures"],
                _localizer["Email.Subscription.TrialExpiring.Steps.Support"],
                _localizer["Email.Subscription.TrialExpiring.Steps.Updates"]
            },
            ClosingMessage = _localizer["Email.Subscription.TrialExpiring.Closing"],
            ActionSignature = _localizer["Email.Subscription.TrialExpiring.Signature"]
        };
    }

    public SubscriptionActionEmailContent GetAutoRenewalContent()
    {
        return new SubscriptionActionEmailContent
        {
            Subject = _localizer["Email.Subscription.AutoRenewal.Subject"],
            Title = _localizer["Email.Subscription.AutoRenewal.Title"],
            Description = _localizer["Email.Subscription.AutoRenewal.Description"],
            CompanyLabel = _localizer["Email.Subscription.CompanyLabel"],
            PlanLabel = _localizer["Email.Subscription.PlanLabel"],
            DateLabel = _localizer["Email.Subscription.RenewalDateLabel"],
            ExpiryDateLabel = _localizer["Email.Subscription.NewExpiryDateLabel"],
            RemainingTimeLabel = _localizer["Email.Subscription.RemainingTimeLabel"],
            DaysLabel = _localizer["Email.Subscription.DaysLabel"],
            WhatThisMeansTitle = _localizer["Email.Subscription.AutoRenewal.WhatThisMeans"],
            WhatThisMeansItems = new[]
            {
                _localizer["Email.Subscription.AutoRenewal.Means.Continued"],
                _localizer["Email.Subscription.AutoRenewal.Means.Uninterrupted"],
                _localizer["Email.Subscription.AutoRenewal.Means.Support"],
                _localizer["Email.Subscription.AutoRenewal.Means.Updates"],
                _localizer["Email.Subscription.AutoRenewal.Means.Secure"]
            },
            ClosingMessage = _localizer["Email.Subscription.AutoRenewal.Closing"],
            ActionSignature = _localizer["Email.Subscription.AutoRenewal.Signature"]
        };
    }

    // ===== ADMIN SECURITY EMAIL CONTENT =====

    public SecurityEmailContent GetEmailChangedContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.EmailChanged.Subject"],
            Title = _localizer["Email.Security.EmailChanged.Title"],
            Description = _localizer["Email.Security.EmailChanged.Description"],
            PreviousEmailLabel = _localizer["Email.Security.PreviousEmailLabel"],
            NewEmailLabel = _localizer["Email.Security.NewEmailLabel"],
            ChangedAtLabel = _localizer["Email.Security.ChangedAtLabel"],
            SecurityNoticeTitle = _localizer["Email.Security.SecurityNoticeTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.EmailChanged.Notice.SentToBoth"],
                _localizer["Email.Security.EmailChanged.Notice.NotYou"],
                _localizer["Email.Security.EmailChanged.Notice.Compromised"]
            },
            NextStepsTitle = _localizer["Email.Security.NextStepsTitle"],
            NextStepsItems = new[]
            {
                _localizer["Email.Security.EmailChanged.Steps.Verify"],
                _localizer["Email.Security.EmailChanged.Steps.Update"],
                _localizer["Email.Security.EmailChanged.Steps.Enable2FA"]
            },
            ClosingMessage = _localizer["Email.Security.ThankYouSecure"]
        };
    }

    public SecurityEmailContent GetPasswordChangedContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.PasswordChanged.Subject"],
            Title = _localizer["Email.Security.PasswordChanged.Title"],
            Description = _localizer["Email.Security.PasswordChanged.Description"],
            ChangedAtLabel = _localizer["Email.Security.ChangedAtLabel"],
            AccountEmailLabel = _localizer["Email.Security.AccountEmailLabel"],
            SecurityNoticeTitle = _localizer["Email.Security.SecurityNoticeTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.PasswordChanged.Notice.NotYou"],
                _localizer["Email.Security.PasswordChanged.Notice.ContactSupport"],
                _localizer["Email.Security.PasswordChanged.Notice.Enable2FA"]
            },
            NextStepsTitle = _localizer["Email.Security.SecurityTipsTitle"],
            NextStepsItems = new[]
            {
                _localizer["Email.Security.Tips.StrongPassword"],
                _localizer["Email.Security.Tips.NeverShare"],
                _localizer["Email.Security.Tips.ChangeRegularly"],
                _localizer["Email.Security.Tips.Enable2FA"]
            },
            ClosingMessage = _localizer["Email.Security.ThankYouSecure"]
        };
    }

    public SecurityEmailContent GetPasswordResetOtpContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.PasswordReset.Subject"],
            Title = _localizer["Email.Security.PasswordReset.Title"],
            Description = _localizer["Email.Security.PasswordReset.Description"],
            ResetPasswordLabel = _localizer["Email.Security.PasswordReset.ResetLabel"],
            ResetInstructions = _localizer["Email.Security.PasswordReset.Instructions"],
            ResetButtonLabel = _localizer["Email.Security.PasswordReset.ButtonLabel"],
            LinkExpiresLabel = _localizer["Email.Security.PasswordReset.LinkExpires"],
            OrUseCodeLabel = _localizer["Email.Security.PasswordReset.OrUseCode"],
            CodeInstructions = _localizer["Email.Security.PasswordReset.CodeInstructions"],
            CodeExpiresLabel = _localizer["Email.Security.PasswordReset.CodeExpires"],
            SecurityAlertTitle = _localizer["Email.Security.SecurityAlertTitle"],
            SecurityAlertItems = new[]
            {
                _localizer["Email.Security.PasswordReset.Alert.NotChanged"],
                _localizer["Email.Security.PasswordReset.Alert.SomeoneTrying"],
                _localizer["Email.Security.PasswordReset.Alert.Enable2FA"],
                _localizer["Email.Security.PasswordReset.Alert.ContactSupport"]
            },
            RequestDetailsLabel = _localizer["Email.Security.RequestDetailsLabel"],
            TimeLabel = _localizer["Email.Security.TimeLabel"],
            IpAddressLabel = _localizer["Email.Security.IpAddressLabel"],
            AccountLabel = _localizer["Email.Security.AccountLabel"],
            ProtectAccountTitle = _localizer["Email.Security.ProtectAccountTitle"],
            ProtectAccountItems = new[]
            {
                _localizer["Email.Security.Protect.NeverShare"],
                _localizer["Email.Security.Protect.NeverAsk"],
                _localizer["Email.Security.Protect.StrongPassword"],
                _localizer["Email.Security.Protect.Enable2FA"],
                _localizer["Email.Security.Protect.KeepUpdated"]
            },
            ClosingMessage = _localizer["Email.Security.PasswordReset.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }

    public SecurityEmailContent GetPasswordResetSuccessContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.PasswordResetSuccess.Subject"],
            Title = _localizer["Email.Security.PasswordResetSuccess.Title"],
            Description = _localizer["Email.Security.PasswordResetSuccess.Description"],
            SuccessMessage = _localizer["Email.Security.PasswordResetSuccess.Message"],
            ChangedAtLabel = _localizer["Email.Security.ChangedAtLabel"],
            IpAddressLabel = _localizer["Email.Security.IpAddressLabel"],
            AccountLabel = _localizer["Email.Security.AccountLabel"],
            SecurityAlertTitle = _localizer["Email.Security.SecurityAlertTitle"],
            SecurityAlertItems = new[]
            {
                _localizer["Email.Security.PasswordResetSuccess.Alert.NotYou"],
                _localizer["Email.Security.PasswordResetSuccess.Alert.ContactSupport"],
                _localizer["Email.Security.PasswordResetSuccess.Alert.Enable2FA"]
            },
            ClosingMessage = _localizer["Email.Security.PasswordResetSuccess.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }

    // ===== 2FA AND BACKUP CODE EMAIL CONTENT =====

    public SecurityEmailContent GetBackupCodesGeneratedContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.BackupCodes.Generated.Subject"],
            Title = _localizer["Email.Security.BackupCodes.Generated.Title"],
            Description = _localizer["Email.Security.BackupCodes.Generated.Description"],
            CodesCountLabel = _localizer["Email.Security.BackupCodes.CountLabel"],
            GeneratedAtLabel = _localizer["Email.Security.GeneratedAtLabel"],
            IpAddressLabel = _localizer["Email.Security.IpAddressLabel"],
            SecurityNoticeTitle = _localizer["Email.Security.ImportantTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.BackupCodes.Notice.SaveSecurely"],
                _localizer["Email.Security.BackupCodes.Notice.OneTimeUse"],
                _localizer["Email.Security.BackupCodes.Notice.Use2FARecovery"],
                _localizer["Email.Security.BackupCodes.Notice.GenerateNew"]
            },
            ClosingMessage = _localizer["Email.Security.BackupCodes.Generated.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }

    public SecurityEmailContent GetBackupCodeUsedContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.BackupCodes.Used.Subject"],
            Title = _localizer["Email.Security.BackupCodes.Used.Title"],
            Description = _localizer["Email.Security.BackupCodes.Used.Description"],
            RemainingCodesLabel = _localizer["Email.Security.BackupCodes.RemainingLabel"],
            UsedAtLabel = _localizer["Email.Security.UsedAtLabel"],
            IpAddressLabel = _localizer["Email.Security.IpAddressLabel"],
            SecurityNoticeTitle = _localizer["Email.Security.SecurityNoticeTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.BackupCodes.Used.Notice.OneUsed"],
                _localizer["Email.Security.BackupCodes.Used.Notice.NotYou"],
                _localizer["Email.Security.BackupCodes.Used.Notice.GenerateNew"]
            },
            ClosingMessage = _localizer["Email.Security.BackupCodes.Used.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }

    public SecurityEmailContent GetBackupCodesLowContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.BackupCodes.Low.Subject"],
            Title = _localizer["Email.Security.BackupCodes.Low.Title"],
            Description = _localizer["Email.Security.BackupCodes.Low.Description"],
            RemainingCodesLabel = _localizer["Email.Security.BackupCodes.RemainingLabel"],
            SecurityNoticeTitle = _localizer["Email.Security.RecommendationTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.BackupCodes.Low.Notice.GenerateSoon"],
                _localizer["Email.Security.BackupCodes.Low.Notice.AvoidLockout"],
                _localizer["Email.Security.BackupCodes.Low.Notice.GoToSettings"]
            },
            ClosingMessage = _localizer["Email.Security.BackupCodes.Low.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }

    public SecurityEmailContent GetBackupCodesDepletedContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.BackupCodes.Depleted.Subject"],
            Title = _localizer["Email.Security.BackupCodes.Depleted.Title"],
            Description = _localizer["Email.Security.BackupCodes.Depleted.Description"],
            SecurityNoticeTitle = _localizer["Email.Security.UrgentTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.BackupCodes.Depleted.Notice.NoCodesLeft"],
                _localizer["Email.Security.BackupCodes.Depleted.Notice.GenerateNow"],
                _localizer["Email.Security.BackupCodes.Depleted.Notice.RiskLockout"]
            },
            ClosingMessage = _localizer["Email.Security.BackupCodes.Depleted.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }

    public SecurityEmailContent Get2FADisabledContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.2FA.Disabled.Subject"],
            Title = _localizer["Email.Security.2FA.Disabled.Title"],
            Description = _localizer["Email.Security.2FA.Disabled.Description"],
            DisabledAtLabel = _localizer["Email.Security.DisabledAtLabel"],
            IpAddressLabel = _localizer["Email.Security.IpAddressLabel"],
            SecurityNoticeTitle = _localizer["Email.Security.SecurityAlertTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.2FA.Disabled.Notice.LessSecure"],
                _localizer["Email.Security.2FA.Disabled.Notice.NotYou"],
                _localizer["Email.Security.2FA.Disabled.Notice.ReEnable"]
            },
            ClosingMessage = _localizer["Email.Security.2FA.Disabled.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }

    public SecurityEmailContent Get2FAResetContent()
    {
        return new SecurityEmailContent
        {
            Subject = _localizer["Email.Security.2FA.Reset.Subject"],
            Title = _localizer["Email.Security.2FA.Reset.Title"],
            Description = _localizer["Email.Security.2FA.Reset.Description"],
            ResetAtLabel = _localizer["Email.Security.ResetAtLabel"],
            IpAddressLabel = _localizer["Email.Security.IpAddressLabel"],
            SecurityNoticeTitle = _localizer["Email.Security.NextStepsTitle"],
            SecurityNoticeItems = new[]
            {
                _localizer["Email.Security.2FA.Reset.Notice.SetupAgain"],
                _localizer["Email.Security.2FA.Reset.Notice.NewQRCode"],
                _localizer["Email.Security.2FA.Reset.Notice.SaveBackupCodes"]
            },
            ClosingMessage = _localizer["Email.Security.2FA.Reset.Closing"],
            SignatureText = _localizer["Email.Security.SecurityTeam"]
        };
    }
}

/// <summary>
/// Container for security email content
/// </summary>
public class SecurityEmailContent
{
    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PreviousEmailLabel { get; set; } = string.Empty;
    public string NewEmailLabel { get; set; } = string.Empty;
    public string ChangedAtLabel { get; set; } = string.Empty;
    public string AccountEmailLabel { get; set; } = string.Empty;
    public string SecurityNoticeTitle { get; set; } = string.Empty;
    public string[] SecurityNoticeItems { get; set; } = Array.Empty<string>();
    public string NextStepsTitle { get; set; } = string.Empty;
    public string[] NextStepsItems { get; set; } = Array.Empty<string>();
    public string ClosingMessage { get; set; } = string.Empty;
    public string SignatureText { get; set; } = string.Empty;
    
    // Password Reset specific
    public string ResetPasswordLabel { get; set; } = string.Empty;
    public string ResetInstructions { get; set; } = string.Empty;
    public string ResetButtonLabel { get; set; } = string.Empty;
    public string LinkExpiresLabel { get; set; } = string.Empty;
    public string OrUseCodeLabel { get; set; } = string.Empty;
    public string CodeInstructions { get; set; } = string.Empty;
    public string CodeExpiresLabel { get; set; } = string.Empty;
    public string SecurityAlertTitle { get; set; } = string.Empty;
    public string[] SecurityAlertItems { get; set; } = Array.Empty<string>();
    public string RequestDetailsLabel { get; set; } = string.Empty;
    public string TimeLabel { get; set; } = string.Empty;
    public string IpAddressLabel { get; set; } = string.Empty;
    public string AccountLabel { get; set; } = string.Empty;
    public string ProtectAccountTitle { get; set; } = string.Empty;
    public string[] ProtectAccountItems { get; set; } = Array.Empty<string>();
    public string SuccessMessage { get; set; } = string.Empty;
    
    // Backup codes specific
    public string CodesCountLabel { get; set; } = string.Empty;
    public string GeneratedAtLabel { get; set; } = string.Empty;
    public string RemainingCodesLabel { get; set; } = string.Empty;
    public string UsedAtLabel { get; set; } = string.Empty;
    public string DisabledAtLabel { get; set; } = string.Empty;
    public string ResetAtLabel { get; set; } = string.Empty;
}

/// <summary>
/// Container for localized email content
/// </summary>
public class EmailLocalizedContent
{
    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string WelcomeMessage { get; set; } = string.Empty;
    public string CompanyNameLabel { get; set; } = string.Empty;
    public string RegistrationDateLabel { get; set; } = string.Empty;
    public string ContactEmailLabel { get; set; } = string.Empty;
    public string UpdatedFieldsLabel { get; set; } = string.Empty;
    public string UpdateDateLabel { get; set; } = string.Empty;
    public string ActivationDateLabel { get; set; } = string.Empty;
    public string DeactivationDateLabel { get; set; } = string.Empty;
    public string DeletionDateLabel { get; set; } = string.Empty;
    public string ReasonLabel { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public string MessageLabel { get; set; } = string.Empty;
    public string NotesLabel { get; set; } = string.Empty;
    public string WhatsNextTitle { get; set; } = string.Empty;
    public string[] WhatsNextItems { get; set; } = Array.Empty<string>();
    public string FeaturesTitle { get; set; } = string.Empty;
    public string[] FeaturesItems { get; set; } = Array.Empty<string>();
    public string ThankYouMessage { get; set; } = string.Empty;
}

/// <summary>
/// Container for common email elements
/// </summary>
public class EmailCommonContent
{
    public string NotificationDetailsTitle { get; set; } = string.Empty;
    public string CompanyLabel { get; set; } = string.Empty;
    public string NotificationTimeLabel { get; set; } = string.Empty;
    public string SystemLabel { get; set; } = string.Empty;
    public string NeedAssistanceTitle { get; set; } = string.Empty;
    public string NeedAssistanceMessage { get; set; } = string.Empty;
    public string EmailLabel { get; set; } = string.Empty;
    public string WebsiteLabel { get; set; } = string.Empty;
    public string AutomatedMessage { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
    public string CopyrightText { get; set; } = string.Empty;
    public string HelloLabel { get; set; } = string.Empty;
    public string NeedHelpLabel { get; set; } = string.Empty;
    public string SupportMessage { get; set; } = string.Empty;
    public string PoweredByText { get; set; } = string.Empty;
}

/// <summary>
/// Container for subscription action email content
/// </summary>
public class SubscriptionActionEmailContent
{
    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CompanyLabel { get; set; } = string.Empty;
    public string PlanLabel { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public string ReasonLabel { get; set; } = string.Empty;
    public string NotesLabel { get; set; } = string.Empty;
    public string ExpiryDateLabel { get; set; } = string.Empty;
    public string DaysRemainingLabel { get; set; } = string.Empty;
    public string RemainingTimeLabel { get; set; } = string.Empty;
    public string DaysLabel { get; set; } = string.Empty;
    public string WhatThisMeansTitle { get; set; } = string.Empty;
    public string[] WhatThisMeansItems { get; set; } = Array.Empty<string>();
    public string NextStepsTitle { get; set; } = string.Empty;
    public string[] NextStepsItems { get; set; } = Array.Empty<string>();
    public string ClosingMessage { get; set; } = string.Empty;
    public string ActionSignature { get; set; } = string.Empty;
    
    // Additional properties for specific email types
    public string UpgradeLabel { get; set; } = string.Empty;
    public string PreviousExpiryLabel { get; set; } = string.Empty;
    public string ExtensionPeriodLabel { get; set; } = string.Empty;
}
