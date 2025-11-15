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
    /// </summary>
    /// <param name="language">Language code (en, ar)</param>
    public void SetCulture(string? language)
    {
        if (string.IsNullOrEmpty(language))
            language = "en"; // Default to English

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
