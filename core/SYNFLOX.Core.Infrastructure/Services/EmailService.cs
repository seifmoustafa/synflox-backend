using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Application.Services;
using Application.DTOs.Company;
using AutoMapper;
using Domain.Interfaces;
using Domain.Enums;

namespace Infrastructure.Services;

/// <summary>
/// Email service using Gmail SMTP with beautiful HTML templates
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILocalizationService _localizer;
    private readonly EmailLocalizationHelper _localizationHelper;
    private readonly ICompanyRepository _companyRepository;
    private readonly IMapper _mapper;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _appPassword;
    
    // Company settings - centralized configuration
    private readonly string _companyName;
    private readonly string _websiteUrl;
    private readonly string _supportEmail;
    private readonly string _systemName;

    public EmailService(IConfiguration configuration, ILocalizationService localizer, ICompanyRepository companyRepository, IMapper mapper)
    {
        _configuration = configuration;
        _localizer = localizer;
        _localizationHelper = new EmailLocalizationHelper(localizer);
        _companyRepository = companyRepository;
        _mapper = mapper;
        
        _smtpHost = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
        _fromEmail = _configuration["EmailSettings:FromEmail"] ?? throw new InvalidOperationException("EmailSettings:FromEmail not configured");
        _fromName = _configuration["EmailSettings:FromName"] ?? "SYNFLOX System";
        _appPassword = _configuration["EmailSettings:Pass"] ?? throw new InvalidOperationException("EmailSettings:Pass not configured");
        
        // Load company settings from configuration
        _companyName = _configuration["CompanySettings:Name"] ?? "SYNFLOX";
        _websiteUrl = _configuration["CompanySettings:WebsiteUrl"] ?? "https://synflox.com";
        _supportEmail = _configuration["CompanySettings:SupportEmail"] ?? _fromEmail;
        _systemName = _configuration["CompanySettings:SystemName"] ?? "Central Licensing System";
    }

    public async Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, bool isTrial, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = isTrial 
            ? _localizationHelper.GetTrialStartedContent() 
            : _localizationHelper.GetSubscriptionActivatedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var startDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{expiryDate:yyyy/MM/dd} - {expiryDate:HH:mm}"
            : $"{expiryDate:MMMM dd, yyyy} at {expiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {startDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(expiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            <br><br>
            <strong>🚀 {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionActivatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionActivatedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var activationDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{expiryDate:yyyy/MM/dd} - {expiryDate:HH:mm}"
            : $"{expiryDate:MMMM dd, yyyy} at {expiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {activationDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(expiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            <br><br>
            <strong>✨ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionExpiredEmailAsync(string toEmail, string companyName, string planName, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionExpiredContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        var nextStepsItems = string.Join("<br>", content.NextStepsItems.Select(item => $"• {item}"));
        
        // Format date based on language
        var expirationDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {expirationDate} UTC
            <br><br>
            <strong>⚠️ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            <strong>🔄 {content.NextStepsTitle}:</strong><br>
            {nextStepsItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#F44336", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string reason, string? language = null, string? notes = null, string? planName = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionSuspendedContent();
        var commonContent = _localizationHelper.GetCommonContent();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        var nextStepsItems = string.Join("<br>", content.NextStepsItems.Select(item => $"• {item}"));
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            {(!string.IsNullOrEmpty(planName) ? $"<strong>📦 {content.PlanLabel}:</strong> {planName}<br>" : "")}
            <strong>📅 {content.DateLabel}:</strong> {DateTime.UtcNow:MMMM dd, yyyy} - {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📝 {content.ReasonLabel}:</strong> {reason}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>⚠️ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            <strong>📋 {content.NextStepsTitle}:</strong><br>
            {nextStepsItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#FF9800", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionRenewedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var renewalDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{newExpiryDate:yyyy/MM/dd} - {newExpiryDate:HH:mm}"
            : $"{newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {renewalDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(newExpiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            {(!string.IsNullOrEmpty(reason) ? $"<br><strong>📝 {content.ReasonLabel}:</strong> {reason}" : "")}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>✨ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlan, string newPlan, DateTime newExpiryDate, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionUpgradedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var upgradeDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{newExpiryDate:yyyy/MM/dd} - {newExpiryDate:HH:mm}"
            : $"{newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📈 {content.UpgradeLabel}:</strong> {oldPlan} → {newPlan}<br>
            <strong>📅 {content.DateLabel}:</strong> {upgradeDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(newExpiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            <br><br>
            <strong>✨ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#9C27B0", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionCanceledEmailAsync(string toEmail, string companyName, string planName, string? language = null, string? reason = null, string? notes = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionCanceledContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        var nextStepsItems = string.Join("<br>", content.NextStepsItems.Select(item => $"• {item}"));
        
        // Format date based on language
        var cancelDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {cancelDate} UTC
            {(!string.IsNullOrEmpty(reason) ? $"<br><strong>📝 {content.ReasonLabel}:</strong> {reason}" : "")}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>📝 {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            <strong>🔄 {content.NextStepsTitle}:</strong><br>
            {nextStepsItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#607D8B", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialStartedEmailAsync(string toEmail, string companyName, string planName, int trialDays, DateTime expiryDate, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetTrialStartedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        var nextStepsItems = string.Join("<br>", content.NextStepsItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var startDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{expiryDate:yyyy/MM/dd} - {expiryDate:HH:mm}"
            : $"{expiryDate:MMMM dd, yyyy} at {expiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {startDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {trialDays} {content.DaysLabel}
            <br><br>
            <strong>🚀 {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            <strong>💡 {content.NextStepsTitle}:</strong><br>
            {nextStepsItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#00BCD4", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialExpiringEmailAsync(string toEmail, string companyName, string planName, int daysRemaining, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetTrialExpiringContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName, daysRemaining);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        var nextStepsItems = string.Join("<br>", content.NextStepsItems.Select(item => $"• {item}"));
        
        // Format date based on language
        var expirationDate = isRtl 
            ? $"{DateTime.UtcNow.AddDays(daysRemaining):yyyy/MM/dd}"
            : $"{DateTime.UtcNow.AddDays(daysRemaining):MMMM dd, yyyy}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>⏰ {content.RemainingTimeLabel}:</strong> {daysRemaining} {content.DaysLabel}<br>
            <strong>📅 {content.ExpiryDateLabel}:</strong> {expirationDate}
            <br><br>
            <strong>🚀 {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            <strong>💳 {content.NextStepsTitle}:</strong><br>
            {nextStepsItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, string.Format(content.Title, daysRemaining), message, "#FF5722", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendAutoRenewalEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetAutoRenewalContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var renewalDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{newExpiryDate:yyyy/MM/dd} - {newExpiryDate:HH:mm}"
            : $"{newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {renewalDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(newExpiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            <br><br>
            <strong>✨ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionPausedEmailAsync(string toEmail, string companyName, string planName, string reason, string? language = null, string? notes = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionPausedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format date based on language
        var pauseDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {pauseDate} UTC<br>
            <strong>📝 {content.ReasonLabel}:</strong> {reason}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>⏸️ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#FF9800", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionResumedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionResumedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var resumeDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDate = isRtl 
            ? $"{newExpiryDate:yyyy/MM/dd} - {newExpiryDate:HH:mm}"
            : $"{newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {resumeDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDate} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(newExpiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            {(!string.IsNullOrEmpty(reason) ? $"<br><strong>📝 {content.ReasonLabel}:</strong> {reason}" : "")}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>🚀 {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialStoppedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetTrialStoppedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var conversionDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{newExpiryDate:yyyy/MM/dd} - {newExpiryDate:HH:mm}"
            : $"{newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {conversionDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(newExpiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            {(!string.IsNullOrEmpty(reason) ? $"<br><strong>📝 {content.ReasonLabel}:</strong> {reason}" : "")}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>🎉 {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#9C27B0", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionExtendedEmailAsync(string toEmail, string companyName, string planName, DateTime oldExpiryDate, DateTime newExpiryDate, int extensionDays, string? language = null, string? reason = null, string? notes = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionExtendedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var extensionDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var oldExpiryStr = isRtl 
            ? $"{oldExpiryDate:yyyy/MM/dd}"
            : $"{oldExpiryDate:MMMM dd, yyyy}";
        var newExpiryStr = isRtl 
            ? $"{newExpiryDate:yyyy/MM/dd} - {newExpiryDate:HH:mm}"
            : $"{newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {extensionDate} UTC<br>
            <strong>⏰ {content.PreviousExpiryLabel}:</strong> {oldExpiryStr}<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {newExpiryStr} UTC<br>
            <strong>⏱️ {content.ExtensionPeriodLabel}:</strong> {extensionDays} {content.DaysLabel}<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(newExpiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            {(!string.IsNullOrEmpty(reason) ? $"<br><strong>📝 {content.ReasonLabel}:</strong> {reason}" : "")}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>✨ {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#2196F3", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionReactivatedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetSubscriptionReactivatedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var subject = string.Format(content.Subject, companyName);
        var whatThisMeansItems = string.Join("<br>", content.WhatThisMeansItems.Select(item => $"• {item}"));
        
        // Format dates based on language
        var reactivationDate = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        var expiryDateStr = isRtl 
            ? $"{newExpiryDate:yyyy/MM/dd} - {newExpiryDate:HH:mm}"
            : $"{newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>🏢 {content.CompanyLabel}:</strong> {companyName}<br>
            <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
            <strong>📅 {content.DateLabel}:</strong> {reactivationDate} UTC<br>
            <strong>⏰ {content.ExpiryDateLabel}:</strong> {expiryDateStr} UTC<br>
            <strong>⏱️ {content.RemainingTimeLabel}:</strong> {(newExpiryDate - DateTime.UtcNow).Days} {content.DaysLabel}
            {(!string.IsNullOrEmpty(reason) ? $"<br><strong>📝 {content.ReasonLabel}:</strong> {reason}" : "")}
            {(!string.IsNullOrEmpty(notes) ? $"<br><strong>💬 {content.NotesLabel}:</strong> {notes}" : "")}
            <br><br>
            <strong>🚀 {content.WhatThisMeansTitle}:</strong><br>
            {whatThisMeansItems}
            <br><br>
            {content.ClosingMessage}
            <br><br>
            <em>{content.ActionSignature}</em>";
        
        var body = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_fromEmail, _appPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - email failure shouldn't break the application
            Console.WriteLine($"Email sending failed: {ex.Message}");
        }
    }

    private string GetLogoUrl()
    {
        // CENTRALIZED: Read logo URL from appsettings.json
        // Change logo in ONE place: appsettings.json -> AppLogo:Url
        return _configuration["AppLogo:Url"];
    }
    
    private string GetLogoStyle()
    {
        // CENTRALIZED: Read logo styling from appsettings.json
        var width = _configuration["AppLogo:Width"] ?? "100";
        var height = _configuration["AppLogo:Height"] ?? "100";
        var isCircular = _configuration["AppLogo:IsCircular"] == "true";
        
        var borderRadius = isCircular ? "border-radius: 50%;" : "";
        
        return $"width: {width}px; height: {height}px; {borderRadius} object-fit: cover; margin-bottom: 20px; box-shadow: 0 4px 12px rgba(0,0,0,0.15); border: 3px solid rgba(255,255,255,0.3);";
    }

    private string BuildEmailTemplate(string companyName, string title, string message, string accentColor)
    {
        var logoUrl = GetLogoUrl();
        var logoStyle = GetLogoStyle();
        var currentYear = DateTime.UtcNow.Year;
        var notificationTime = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm");
        
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f8fafc; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {accentColor} 0%, {accentColor}dd 100%); padding: 40px; text-align: center;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $@"<img src=""{logoUrl}"" alt=""SYNFLOX Logo"" style=""{logoStyle}"" />")}
                            <h1 style=""color: white; margin: 0; font-size: 36px; font-weight: 700; letter-spacing: -0.5px;"">SYNFLOX</h1>
                            <p style=""color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px; font-weight: 500;"">Central Licensing System</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 50px 40px;"">
                            <h2 style=""color: #1a202c; margin: 0 0 25px 0; font-size: 28px; font-weight: 600; line-height: 1.3;"">{title}</h2>
                            <p style=""color: #4a5568; line-height: 1.7; margin: 0 0 20px 0; font-size: 16px;"">Hello <strong style=""color: #2d3748;"">{companyName}</strong>,</p>
                            <div style=""color: #4a5568; line-height: 1.7; margin: 0 0 30px 0; font-size: 16px;"">{message}</div>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 35px 0; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;"">
                                <tr>
                                    <td style=""background: linear-gradient(90deg, {accentColor}15 0%, {accentColor}08 100%); padding: 20px; border-left: 4px solid {accentColor};"">
                                        <h3 style=""margin: 0 0 15px 0; color: #2d3748; font-size: 18px; font-weight: 600;"">📋 Notification Details</h3>
                                        <div style=""color: #4a5568; font-size: 14px; line-height: 1.6;"">
                                            <p style=""margin: 5px 0;""><strong>Company:</strong> {companyName}</p>
                                            <p style=""margin: 5px 0;""><strong>Notification Time:</strong> {notificationTime} UTC</p>
                                            <p style=""margin: 5px 0;""><strong>System:</strong> SYNFLOX Central Licensing</p>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 35px 0; background-color: #f7fafc; border-radius: 8px; border: 1px solid #e2e8f0;"">
                                <tr>
                                    <td style=""padding: 25px;"">
                                        <h3 style=""margin: 0 0 15px 0; color: #2d3748; font-size: 16px; font-weight: 600;"">🆘 Need Assistance?</h3>
                                        <p style=""margin: 0 0 15px 0; color: #4a5568; font-size: 14px; line-height: 1.6;"">
                                            Our support team is here to help you with any questions or concerns.
                                        </p>
                                        <p style=""margin: 0; color: #4a5568; font-size: 14px;"">
                                            📧 Email: <a href=""mailto:{_supportEmail}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_supportEmail}</a><br>
                                            🌐 Website: <a href=""{_websiteUrl}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_websiteUrl.Replace("https://", "").Replace("http://", "")}</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            
                            <div style=""margin-top: 40px; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                                <p style=""color: #a0aec0; font-size: 13px; margin: 0; line-height: 1.6; text-align: center;"">
                                    This is an automated notification from {_companyName} {_systemName}.<br>
                                    Please do not reply to this email. For support, use the contact information above.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: linear-gradient(90deg, #f7fafc 0%, #edf2f7 100%); padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;"">
                            <p style=""margin: 0 0 10px 0; color: #718096; font-size: 14px; font-weight: 500;"">
                                {_companyName} - Professional Licensing Solutions
                            </p>
                            <p style=""margin: 0; color: #a0aec0; font-size: 12px;"">
                                &copy; {currentYear} {_companyName}. All rights reserved. | Powered by Advanced Technology
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Build RTL-aware localized email template for subscription actions
    /// Supports Arabic (RTL) and English (LTR) with proper text alignment
    /// </summary>
    private string BuildSubscriptionActionEmailTemplate(string companyName, string title, string message, string accentColor, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var commonContent = _localizationHelper.GetCommonContent();
        var isRtl = _localizationHelper.IsRtl();
        var direction = _localizationHelper.GetTextDirection();
        var textAlign = _localizationHelper.GetTextAlign();
        var borderSide = isRtl ? "border-right" : "border-left";
        
        var logoUrl = GetLogoUrl();
        var logoStyle = GetLogoStyle();
        var currentYear = DateTime.UtcNow.Year;
        
        // Format date based on language
        var notificationTime = isRtl 
            ? DateTime.UtcNow.ToString("yyyy/MM/dd - HH:mm")
            : DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm");
        
        var fontFamily = isRtl 
            ? "'Segoe UI', 'Arabic Typesetting', 'Traditional Arabic', Tahoma, Arial, sans-serif"
            : "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
        
        return $@"<!DOCTYPE html>
<html dir=""{direction}"" lang=""{language ?? "en"}"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <!--[if mso]>
    <style type=""text/css"">
        table {{ border-collapse: collapse; }}
        .mobile-padding {{ padding: 20px !important; }}
    </style>
    <![endif]-->
    <style type=""text/css"">
        @media only screen and (max-width: 620px) {{
            .email-container {{ width: 100% !important; max-width: 100% !important; }}
            .mobile-padding {{ padding: 25px 15px !important; }}
            .header-padding {{ padding: 30px 20px !important; }}
            .logo-title {{ font-size: 28px !important; }}
            .content-title {{ font-size: 22px !important; }}
            .content-text {{ font-size: 14px !important; }}
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; font-family: {fontFamily}; background-color: #f8fafc; direction: {direction}; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color: #f8fafc; padding: 10px;"">
        <tr>
            <td align=""center"" style=""padding: 10px;"">
                <table class=""email-container"" width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width: 600px; width: 100%; background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);"">
                    <tr>
                        <td class=""header-padding"" style=""background: linear-gradient(135deg, {accentColor} 0%, {accentColor}dd 100%); padding: 40px 20px; text-align: center;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $@"<img src=""{logoUrl}"" alt=""SYNFLOX Logo"" style=""{logoStyle} max-width: 80px;"" />")}
                            <h1 class=""logo-title"" style=""color: white; margin: 0; font-size: 32px; font-weight: 700; letter-spacing: -0.5px;"">SYNFLOX</h1>
                            <p style=""color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 14px; font-weight: 500;"">{(isRtl ? "نظام الترخيص المركزي" : "Central Licensing System")}</p>
                        </td>
                    </tr>
                    <tr>
                        <td class=""mobile-padding"" style=""padding: 40px 30px; text-align: {textAlign};"">
                            <h2 class=""content-title"" style=""color: #1a202c; margin: 0 0 25px 0; font-size: 24px; font-weight: 600; line-height: 1.3; text-align: {textAlign};"">{title}</h2>
                            <p class=""content-text"" style=""color: #4a5568; line-height: 1.7; margin: 0 0 20px 0; font-size: 15px; text-align: {textAlign};"">{commonContent.HelloLabel} <strong style=""color: #2d3748;"">{companyName}</strong>,</p>
                            <div class=""content-text"" style=""color: #4a5568; line-height: 1.8; margin: 0 0 30px 0; font-size: 15px; text-align: {textAlign};"">{message}</div>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin: 25px 0; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;"">
                                <tr>
                                    <td style=""background: linear-gradient(90deg, {accentColor}15 0%, {accentColor}08 100%); padding: 20px; {borderSide}: 4px solid {accentColor};"">
                                        <h3 style=""margin: 0 0 15px 0; color: #2d3748; font-size: 18px; font-weight: 600; text-align: {textAlign};"">{(isRtl ? "📋" : "📋")} {commonContent.NotificationDetailsTitle}</h3>
                                        <div style=""color: #4a5568; font-size: 14px; line-height: 1.6; text-align: {textAlign};"">
                                            <p style=""margin: 5px 0;""><strong>{commonContent.CompanyLabel}:</strong> {companyName}</p>
                                            <p style=""margin: 5px 0;""><strong>{commonContent.NotificationTimeLabel}:</strong> {notificationTime} UTC</p>
                                            <p style=""margin: 5px 0;""><strong>{commonContent.SystemLabel}:</strong> SYNFLOX</p>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 35px 0; background-color: #f7fafc; border-radius: 8px; border: 1px solid #e2e8f0;"">
                                <tr>
                                    <td style=""padding: 25px; text-align: {textAlign};"">
                                        <h3 style=""margin: 0 0 15px 0; color: #2d3748; font-size: 16px; font-weight: 600;"">{(isRtl ? "🆘" : "🆘")} {commonContent.NeedAssistanceTitle}</h3>
                                        <p style=""margin: 0 0 15px 0; color: #4a5568; font-size: 14px; line-height: 1.6;"">
                                            {commonContent.NeedAssistanceMessage}
                                        </p>
                                        <p style=""margin: 0; color: #4a5568; font-size: 14px;"">
                                            {(isRtl ? "📧" : "📧")} {commonContent.EmailLabel}: <a href=""mailto:{_supportEmail}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_supportEmail}</a><br>
                                            {(isRtl ? "🌐" : "🌐")} {commonContent.WebsiteLabel}: <a href=""{_websiteUrl}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_websiteUrl.Replace("https://", "").Replace("http://", "")}</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            
                            <div style=""margin-top: 40px; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                                <p style=""color: #a0aec0; font-size: 13px; margin: 0; line-height: 1.6; text-align: center;"">
                                    {commonContent.AutomatedMessage}
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: linear-gradient(90deg, #f7fafc 0%, #edf2f7 100%); padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;"">
                            <p style=""margin: 0 0 10px 0; color: #718096; font-size: 14px; font-weight: 500;"">
                                {commonContent.FooterText}
                            </p>
                            <p style=""margin: 0; color: #a0aec0; font-size: 12px;"">
                                {string.Format(commonContent.CopyrightText, currentYear)}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    /// <summary>
    /// Build localized email template with proper culture support
    /// </summary>
    private string BuildLocalizedEmailTemplate(string companyName, string title, string message, string accentColor, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var commonContent = _localizationHelper.GetCommonContent();
        
        var logoUrl = GetLogoUrl();
        var logoStyle = GetLogoStyle();
        var currentYear = DateTime.UtcNow.Year;
        var notificationTime = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm");
        
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f8fafc; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {accentColor} 0%, {accentColor}dd 100%); padding: 40px; text-align: center;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $@"<img src=""{logoUrl}"" alt=""SYNFLOX Logo"" style=""{logoStyle}"" />")}
                            <h1 style=""color: white; margin: 0; font-size: 36px; font-weight: 700; letter-spacing: -0.5px;"">SYNFLOX</h1>
                            <p style=""color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px; font-weight: 500;"">Central Licensing System</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 50px 40px;"">
                            <h2 style=""color: #1a202c; margin: 0 0 25px 0; font-size: 28px; font-weight: 600; line-height: 1.3;"">{title}</h2>
                            <p style=""color: #4a5568; line-height: 1.7; margin: 0 0 20px 0; font-size: 16px;"">Hello <strong style=""color: #2d3748;"">{companyName}</strong>,</p>
                            <div style=""color: #4a5568; line-height: 1.7; margin: 0 0 30px 0; font-size: 16px;"">{message}</div>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 35px 0; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;"">
                                <tr>
                                    <td style=""background: linear-gradient(90deg, {accentColor}15 0%, {accentColor}08 100%); padding: 20px; border-left: 4px solid {accentColor};"">
                                        <h3 style=""margin: 0 0 15px 0; color: #2d3748; font-size: 18px; font-weight: 600;"">{commonContent.NotificationDetailsTitle}</h3>
                                        <div style=""color: #4a5568; font-size: 14px; line-height: 1.6;"">
                                            <p style=""margin: 5px 0;""><strong>{commonContent.CompanyLabel}:</strong> {companyName}</p>
                                            <p style=""margin: 5px 0;""><strong>{commonContent.NotificationTimeLabel}:</strong> {notificationTime} UTC</p>
                                            <p style=""margin: 5px 0;""><strong>{commonContent.SystemLabel}:</strong> {_companyName} {_systemName}</p>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 35px 0; background-color: #f7fafc; border-radius: 8px; border: 1px solid #e2e8f0;"">
                                <tr>
                                    <td style=""padding: 25px;"">
                                        <h3 style=""margin: 0 0 15px 0; color: #2d3748; font-size: 16px; font-weight: 600;"">{commonContent.NeedAssistanceTitle}</h3>
                                        <p style=""margin: 0 0 15px 0; color: #4a5568; font-size: 14px; line-height: 1.6;"">
                                            {commonContent.NeedAssistanceMessage}
                                        </p>
                                        <p style=""margin: 0; color: #4a5568; font-size: 14px;"">
                                            📧 {commonContent.EmailLabel}: <a href=""mailto:{_supportEmail}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_supportEmail}</a><br>
                                            🌐 {commonContent.WebsiteLabel}: <a href=""{_websiteUrl}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_websiteUrl.Replace("https://", "").Replace("http://", "")}</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            
                            <div style=""margin-top: 40px; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                                <p style=""color: #a0aec0; font-size: 13px; margin: 0; line-height: 1.6; text-align: center;"">
                                    {commonContent.AutomatedMessage}
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: linear-gradient(90deg, #f7fafc 0%, #edf2f7 100%); padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;"">
                            <p style=""margin: 0 0 10px 0; color: #718096; font-size: 14px; font-weight: 500;"">
                                {commonContent.FooterText}
                            </p>
                            <p style=""margin: 0; color: #a0aec0; font-size: 12px;"">
                                {string.Format(commonContent.CopyrightText, currentYear)}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    // ===== COMPANY CRUD EMAIL METHODS =====

    public async Task SendCompanyCreatedEmailAsync(string toEmail, string companyName, string adminName, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetCompanyCreatedContent();
        
        var subject = string.Format(content.Subject, companyName);
        var title = content.Title;
        
        var whatsNextItems = string.Join("<br>", content.WhatsNextItems.Select(item => $"• {item}"));
        var featuresItems = string.Join("<br>", content.FeaturesItems.Select(item => $"• {item}"));
        
        var message = $@"{content.WelcomeMessage}
            <br><br>
            <strong>🏢 {content.CompanyNameLabel}:</strong> {companyName}<br>
            <strong>📅 {content.RegistrationDateLabel}:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📧 {content.ContactEmailLabel}:</strong> {toEmail}
            <br><br>
            <strong>{content.WhatsNextTitle}</strong><br>
            {whatsNextItems}
            <br><br>
            <strong>{content.FeaturesTitle}:</strong><br>
            {featuresItems}
            <br><br>
            {content.ThankYouMessage}";
        
        var body = BuildLocalizedEmailTemplate(companyName, title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyUpdatedEmailAsync(string toEmail, string companyName, string updatedFields, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetCompanyUpdatedContent();
        
        var subject = string.Format(content.Subject, companyName);
        var title = content.Title;
        
        var message = $@"{content.WelcomeMessage}
            <br><br>
            <strong>🏢 {content.CompanyNameLabel}:</strong> {companyName}<br>
            <strong>📅 {content.UpdateDateLabel}:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📝 {content.UpdatedFieldsLabel}:</strong> {updatedFields}
            <br><br>
            <strong>✅ Changes Applied:</strong><br>
            • Company information has been updated<br>
            • All systems have been synchronized<br>
            • Changes are effective immediately<br>
            • Your subscriptions remain active
            <br><br>
            If you did not make these changes, please contact support immediately.";
        
        var body = BuildLocalizedEmailTemplate(companyName, title, message, "#2196F3", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyActivatedEmailAsync(string toEmail, string companyName, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetCompanyActivatedContent();
        
        var subject = string.Format(content.Subject, companyName);
        var title = content.Title;
        
        var message = $@"{content.WelcomeMessage}
            <br><br>
            <strong>🏢 {content.CompanyNameLabel}:</strong> {companyName}<br>
            <strong>📅 {content.ActivationDateLabel}:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📧 Contact Email:</strong> {toEmail}
            <br><br>
            <strong>🚀 You now have access to:</strong><br>
            • Full SYNFLOX functionality<br>
            • All subscription management features<br>
            • Complete licensing capabilities<br>
            • Priority technical support<br>
            • Advanced analytics and reporting
            <br><br>
            Your company is now fully operational in SYNFLOX!";
        
        var body = BuildLocalizedEmailTemplate(companyName, title, message, "#4CAF50", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyDeactivatedEmailAsync(string toEmail, string companyName, string reason, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetCompanyDeactivatedContent();
        
        var subject = string.Format(content.Subject, companyName);
        var title = content.Title;
        
        var message = $@"{content.WelcomeMessage}
            <br><br>
            <strong>🏢 {content.CompanyNameLabel}:</strong> {companyName}<br>
            <strong>📅 {content.DeactivationDateLabel}:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📝 {content.ReasonLabel}:</strong> {reason}
            <br><br>
            <strong>⚠️ What this means:</strong><br>
            • Access to SYNFLOX services is suspended<br>
            • All subscriptions are temporarily inactive<br>
            • Your data remains safe and secure<br>
            • Contact support for reactivation
            <br><br>
            Please contact our support team to resolve this matter and reactivate your company.";
        
        var body = BuildLocalizedEmailTemplate(companyName, title, message, "#FF9800", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyDeletedEmailAsync(string toEmail, string companyName, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetCompanyDeletedContent();
        
        var subject = string.Format(content.Subject, companyName);
        var title = content.Title;
        
        var message = $@"{content.WelcomeMessage}
            <br><br>
            <strong>🏢 {content.CompanyNameLabel}:</strong> {companyName}<br>
            <strong>📅 {content.DeletionDateLabel}:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📧 Contact Email:</strong> {toEmail}
            <br><br>
            <strong>📝 What has been deleted:</strong><br>
            • Company profile and settings<br>
            • All subscription data<br>
            • User accounts and permissions<br>
            • Analytics and reporting data<br>
            • All associated licenses
            <br><br>
            <strong>🔄 Data Recovery:</strong><br>
            • Data can be recovered within 30 days<br>
            • Contact support immediately if this was a mistake<br>
            • After 30 days, deletion is permanent
            <br><br>
            Thank you for using SYNFLOX. We're sorry to see you go!";
        
        var body = BuildLocalizedEmailTemplate(companyName, title, message, "#607D8B", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    // ===== ADMIN SECURITY NOTIFICATION EMAILS =====

    public async Task SendEmailChangedNotificationAsync(string oldEmail, string newEmail, string adminName, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetEmailChangedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var securityNoticeItems = string.Join("<br>", content.SecurityNoticeItems.Select(item => $"• {item}"));
        var nextStepsItems = string.Join("<br>", content.NextStepsItems.Select(item => $"• {item}"));
        
        var changedAt = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>📧 {content.PreviousEmailLabel}:</strong> {oldEmail}<br>
            <strong>✅ {content.NewEmailLabel}:</strong> {newEmail}<br>
            <strong>📅 {content.ChangedAtLabel}:</strong> {changedAt} UTC
            <br><br>
            <strong>🔒 {content.SecurityNoticeTitle}:</strong><br>
            {securityNoticeItems}
            <br><br>
            <strong>🛡️ {content.NextStepsTitle}:</strong><br>
            {nextStepsItems}
            <br><br>
            {content.ClosingMessage}";
        
        var body = BuildSubscriptionActionEmailTemplate(adminName, content.Title, message, "#2196F3", language);
        
        // Send to BOTH old and new email addresses
        await SendEmailAsync(oldEmail, content.Subject, body);
        await SendEmailAsync(newEmail, content.Subject, body);
    }

    public async Task SendPasswordChangedNotificationAsync(string toEmail, string adminName, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        var content = _localizationHelper.GetPasswordChangedContent();
        var isRtl = _localizationHelper.IsRtl();
        
        var securityNoticeItems = string.Join("<br>", content.SecurityNoticeItems.Select(item => $"• {item}"));
        var nextStepsItems = string.Join("<br>", content.NextStepsItems.Select(item => $"• {item}"));
        
        var changedAt = isRtl 
            ? $"{DateTime.UtcNow:yyyy/MM/dd} - {DateTime.UtcNow:HH:mm}"
            : $"{DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm}";
        
        var message = $@"{content.Description}
            <br><br>
            <strong>📅 {content.ChangedAtLabel}:</strong> {changedAt} UTC<br>
            <strong>📧 {content.AccountEmailLabel}:</strong> {toEmail}
            <br><br>
            <strong>🔒 {content.SecurityNoticeTitle}:</strong><br>
            {securityNoticeItems}
            <br><br>
            <strong>🛡️ {content.NextStepsTitle}:</strong><br>
            {nextStepsItems}
            <br><br>
            {content.ClosingMessage}";
        
        var body = BuildSubscriptionActionEmailTemplate(adminName, content.Title, message, "#FF9800", language);
        await SendEmailAsync(toEmail, content.Subject, body);
    }

    public async Task SendPasswordResetOtpEmailAsync(string toEmail, string adminName, string otpCode, string magicLinkToken, int expiryMinutes, string ipAddress, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        
        var subject = "Security Alert: Password Reset Request for Your SYNFLOX Account";
        var title = "Password Reset Request";
        
        // Build magic link URL using FRONTEND base URL
        var frontendBaseUrl = _configuration["FrontendBaseUrl"];
        var magicLinkUrl = $"{frontendBaseUrl}/reset-password?token={magicLinkToken}";
        
        var message = $@"Hi {adminName},
            <br><br>
            We received a request to reset the password for your SYNFLOX administrator account. If you made this request, you can reset your password using one of the methods below.
            <br><br>
            <div style='background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 24px; margin: 24px 0;'>
                <h3 style='margin: 0 0 16px 0; color: #1a202c; font-size: 17px; font-weight: 600;'>Reset Your Password</h3>
                <p style='margin: 0 0 20px 0; color: #4a5568; font-size: 14px; line-height: 1.6;'>
                    For your security, you can choose to reset your password using a secure link or by entering a verification code manually.
                </p>
                <div style='text-align: center; margin: 24px 0;'>
                    <a href='{magicLinkUrl}' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-size: 15px; font-weight: 600; box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);'>
                        Reset Password
                    </a>
                </div>
                <p style='margin: 16px 0 0 0; color: #6c757d; font-size: 13px; text-align: center; line-height: 1.5;'>
                    This link will expire in {expiryMinutes} minutes.<br>
                    If the button doesn't work, copy and paste this link into your browser:<br>
                    <span style='color: #667eea; word-break: break-all;'>{magicLinkUrl}</span>
                </p>
            </div>
            <br>
            <div style='background-color: #ffffff; border: 1px solid #dee2e6; border-radius: 8px; padding: 24px; margin: 24px 0;'>
                <h3 style='margin: 0 0 16px 0; color: #1a202c; font-size: 17px; font-weight: 600;'>Or Use This Verification Code</h3>
                <p style='margin: 0 0 16px 0; color: #4a5568; font-size: 14px; line-height: 1.6;'>
                    If you prefer, you can reset your password by entering this verification code on the password reset page:
                </p>
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0;'>
                    <div style='font-size: 36px; font-weight: 700; color: white; letter-spacing: 8px; font-family: Consolas, Monaco, monospace;'>{otpCode}</div>
                </div>
                <p style='margin: 16px 0 0 0; color: #6c757d; font-size: 13px; text-align: center;'>
                    This code will expire in {expiryMinutes} minutes
                </p>
            </div>
            <br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Security Alert</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    <strong>If you didn't request this password reset, please take action immediately:</strong><br>
                    • Your password has NOT been changed yet<br>
                    • Someone may be trying to access your account<br>
                    • We recommend securing your account by enabling Two-Factor Authentication<br>
                    • Contact our support team if you notice any suspicious activity
                </p>
            </div>
            <br>
            <div style='background-color: #f8f9fa; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 0 0 8px 0; color: #4a5568; font-size: 13px;'><strong>Request Details:</strong></p>
                <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                    <strong>Time:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                    <strong>IP Address:</strong> {ipAddress}<br>
                    <strong>Account:</strong> {toEmail}
                </p>
            </div>
            <br>
            <div style='background-color: #e7f3ff; border-left: 4px solid #0066cc; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #004085; font-size: 14px; font-weight: 600;'>🛡️ Protect Your Account</p>
                <p style='margin: 0; color: #004085; font-size: 13px; line-height: 1.6;'>
                    • Never share your password or verification codes with anyone<br>
                    • SYNFLOX will never ask you for your password via email<br>
                    • Use a strong, unique password that you don't use elsewhere<br>
                    • Enable Two-Factor Authentication for an extra layer of security<br>
                    • Keep your recovery information up to date
                </p>
            </div>
            <br>
            <p style='margin: 24px 0 0 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                If you have any questions or concerns about your account security, please don't hesitate to contact our support team.
            </p>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#667eea", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetSuccessEmailAsync(string toEmail, string adminName, string ipAddress, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        
        var subject = "Your SYNFLOX Password Has Been Changed";
        var title = "Password Changed Successfully";
        
        var message = $@"Hi {adminName},
            <br><br>
            This email confirms that the password for your SYNFLOX administrator account was successfully changed.
            <br><br>
            <div style='background-color: #d4edda; border-left: 4px solid #28a745; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #155724; font-size: 14px; font-weight: 600;'>✅ Password Changed</p>
                <p style='margin: 0; color: #155724; font-size: 13px; line-height: 1.6;'>
                    Your password has been successfully updated. You can now sign in to your account using your new password.
                </p>
            </div>
            <br>
            <div style='background-color: #f8f9fa; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 0 0 8px 0; color: #4a5568; font-size: 13px;'><strong>Change Details:</strong></p>
                <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                    <strong>Time:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                    <strong>IP Address:</strong> {ipAddress}<br>
                    <strong>Account:</strong> {toEmail}
                </p>
            </div>
            <br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Didn't Make This Change?</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    If you didn't change your password, <strong>someone else may have access to your account.</strong> Please take these steps immediately:<br><br>
                    <strong>1.</strong> Reset your password again using a device and network you trust<br>
                    <strong>2.</strong> Contact our support team immediately<br>
                    <strong>3.</strong> Review your recent account activity for suspicious behavior<br>
                    <strong>4.</strong> Enable Two-Factor Authentication for added security
                </p>
            </div>
            <br>
            <div style='background-color: #e7f3ff; border-left: 4px solid #0066cc; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #004085; font-size: 14px; font-weight: 600;'>�️ Keep Your Account Secure</p>
                <p style='margin: 0; color: #004085; font-size: 13px; line-height: 1.6;'>
                    <strong>Protect your account with these security best practices:</strong><br><br>
                    • <strong>Enable Two-Factor Authentication</strong> – Add an extra layer of protection<br>
                    • <strong>Use a strong password</strong> – Combine uppercase, lowercase, numbers, and symbols<br>
                    • <strong>Never reuse passwords</strong> – Use a unique password for each account<br>
                    • <strong>Keep recovery info updated</strong> – Ensure your backup email is current<br>
                    • <strong>Be cautious of phishing</strong> – SYNFLOX will never ask for your password via email
                </p>
            </div>
            <br>
            <p style='margin: 24px 0 0 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                If you have any questions or concerns about your account security, please contact our support team.
            </p>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#28a745", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    // ===== 2FA AND BACKUP CODE SECURITY EMAILS =====

    public async Task SendBackupCodesGeneratedEmailAsync(string toEmail, string adminName, int codesCount, string ipAddress, string? language = null)
    {
        _localizationHelper.SetCulture(language);
        
        var subject = "🔐 SYNFLOX Backup Codes Generated";
        var title = "New Backup Codes Created";
        
        var message = $@"Hi {adminName},
            <br><br>
            This email confirms that <strong>{codesCount} new backup codes</strong> were generated for your SYNFLOX administrator account.
            <br><br>
            <div style='background-color: #d4edda; border-left: 4px solid #28a745; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #155724; font-size: 14px; font-weight: 600;'>✅ Backup Codes Created</p>
                <p style='margin: 0; color: #155724; font-size: 13px; line-height: 1.6;'>
                    Your backup codes have been generated and are ready to use. These codes can be used to access your account if you lose access to your authenticator app.
                </p>
            </div>
            <br>
            <div style='background-color: #f8f9fa; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 0 0 8px 0; color: #4a5568; font-size: 13px;'><strong>Generation Details:</strong></p>
                <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                    <strong>Time:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                    <strong>IP Address:</strong> {ipAddress}<br>
                    <strong>Codes Generated:</strong> {codesCount}<br>
                    <strong>Account:</strong> {toEmail}
                </p>
            </div>
            <br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Important Security Notes</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    <strong>• Store your backup codes safely</strong> – Keep them in a secure location (password manager, secure notes)<br>
                    <strong>• Each code can only be used once</strong> – Once you use a code, it becomes invalid<br>
                    <strong>• Generate new codes when running low</strong> – Don't wait until all codes are used<br>
                    <strong>• Didn't request this?</strong> – Contact support immediately if you didn't generate these codes
                </p>
            </div>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#28a745", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendBackupCodeUsedEmailAsync(string toEmail, string adminName, int remainingCodes, string ipAddress, string? language = null)
    {
        _localizationHelper.SetCulture(language);
        
        var subject = "🔓 SYNFLOX Backup Code Used";
        var title = "Backup Code Authentication";
        
        var urgencyLevel = remainingCodes <= 2 ? "critical" : remainingCodes <= 5 ? "warning" : "info";
        var urgencyColor = remainingCodes <= 2 ? "#dc3545" : remainingCodes <= 5 ? "#ffc107" : "#17a2b8";
        var urgencyIcon = remainingCodes <= 2 ? "🚨" : remainingCodes <= 5 ? "⚠️" : "ℹ️";
        
        var message = $@"Hi {adminName},
            <br><br>
            A backup code was just used to sign in to your SYNFLOX administrator account.
            <br><br>
            <div style='background-color: #e7f3ff; border-left: 4px solid #0066cc; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #004085; font-size: 14px; font-weight: 600;'>🔓 Backup Code Used</p>
                <p style='margin: 0; color: #004085; font-size: 13px; line-height: 1.6;'>
                    One of your backup codes was successfully used for authentication. You have <strong>{remainingCodes} backup code{(remainingCodes != 1 ? "s" : "")} remaining</strong>.
                </p>
            </div>
            <br>
            <div style='background-color: #f8f9fa; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 0 0 8px 0; color: #4a5568; font-size: 13px;'><strong>Login Details:</strong></p>
                <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                    <strong>Time:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                    <strong>IP Address:</strong> {ipAddress}<br>
                    <strong>Remaining Codes:</strong> {remainingCodes}<br>
                    <strong>Account:</strong> {toEmail}
                </p>
            </div>
            <br>
            <div style='background-color: {(remainingCodes <= 2 ? "#f8d7da" : remainingCodes <= 5 ? "#fff3cd" : "#d1ecf1")}; border-left: 4px solid {urgencyColor}; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: {(remainingCodes <= 2 ? "#721c24" : remainingCodes <= 5 ? "#856404" : "#0c5460")}; font-size: 14px; font-weight: 600;'>{urgencyIcon} {(remainingCodes <= 2 ? "Critical: Generate New Codes Now!" : remainingCodes <= 5 ? "Warning: Low on Backup Codes" : "Recommendation")}</p>
                <p style='margin: 0; color: {(remainingCodes <= 2 ? "#721c24" : remainingCodes <= 5 ? "#856404" : "#0c5460")}; font-size: 13px; line-height: 1.6;'>
                    {(remainingCodes <= 2 
                        ? "<strong>You have 2 or fewer backup codes left!</strong><br>Generate new backup codes immediately to maintain account recovery options. Go to your account security settings to create new codes." 
                        : remainingCodes <= 5 
                            ? "<strong>You're running low on backup codes.</strong><br>We recommend generating new backup codes soon to ensure you always have recovery options available." 
                            : "Monitor your remaining backup codes and generate new ones when needed to maintain secure account recovery options.")}
                </p>
            </div>
            <br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Didn't Use This Code?</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    If you didn't use a backup code to sign in, <strong>someone else may have access to your codes.</strong><br><br>
                    <strong>1.</strong> Change your password immediately<br>
                    <strong>2.</strong> Generate new backup codes<br>
                    <strong>3.</strong> Contact our support team<br>
                    <strong>4.</strong> Review your recent account activity
                </p>
            </div>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, urgencyColor, language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendBackupCodesLowEmailAsync(string toEmail, string adminName, int remainingCodes, string? language = null)
    {
        _localizationHelper.SetCulture(language);
        
        var subject = "⚠️ SYNFLOX Backup Codes Running Low";
        var title = "Backup Codes Low Warning";
        
        var message = $@"Hi {adminName},
            <br><br>
            This is a friendly reminder that you have <strong>only {remainingCodes} backup code{(remainingCodes != 1 ? "s" : "")} remaining</strong> for your SYNFLOX administrator account.
            <br><br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Action Required</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    You should generate new backup codes soon to ensure you always have recovery options if you lose access to your authenticator app.
                </p>
            </div>
            <br>
            <div style='background-color: #e7f3ff; border-left: 4px solid #0066cc; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #004085; font-size: 14px; font-weight: 600;'>📝 How to Generate New Codes</p>
                <p style='margin: 0; color: #004085; font-size: 13px; line-height: 1.6;'>
                    <strong>1.</strong> Sign in to your SYNFLOX account<br>
                    <strong>2.</strong> Go to Profile → Security Settings<br>
                    <strong>3.</strong> Click &quot;Generate New Backup Codes&quot;<br>
                    <strong>4.</strong> Save the new codes in a secure location
                </p>
            </div>
            <br>
            <div style='background-color: #f8f9fa; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 0 0 8px 0; color: #4a5568; font-size: 13px;'><strong>Current Status:</strong></p>
                <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                    <strong>Remaining Codes:</strong> {remainingCodes}<br>
                    <strong>Account:</strong> {toEmail}
                </p>
            </div>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#ffc107", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendBackupCodesDepletedEmailAsync(string toEmail, string adminName, string? language = null)
    {
        _localizationHelper.SetCulture(language);
        
        var subject = "🚨 URGENT: SYNFLOX Backup Codes Depleted";
        var title = "All Backup Codes Used";
        
        var message = $@"Hi {adminName},
            <br><br>
            <strong>This is an urgent security alert.</strong> You have used all of your backup codes for your SYNFLOX administrator account.
            <br><br>
            <div style='background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #721c24; font-size: 14px; font-weight: 600;'>🚨 Critical: No Backup Codes Remaining</p>
                <p style='margin: 0; color: #721c24; font-size: 13px; line-height: 1.6;'>
                    <strong>You have 0 backup codes left!</strong> If you lose access to your authenticator app, you will not be able to recover your account using backup codes.
                </p>
            </div>
            <br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Generate New Codes Immediately</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    <strong>Follow these steps NOW to restore your account recovery options:</strong><br><br>
                    <strong>1.</strong> Sign in to your SYNFLOX account<br>
                    <strong>2.</strong> Go to Profile → Security Settings<br>
                    <strong>3.</strong> Click &quot;Generate New Backup Codes&quot;<br>
                    <strong>4.</strong> Save the new codes in a secure location (password manager recommended)<br>
                    <strong>5.</strong> Print or write down the codes as a backup
                </p>
            </div>
            <br>
            <div style='background-color: #e7f3ff; border-left: 4px solid #0066cc; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #004085; font-size: 14px; font-weight: 600;'>🛡️ Protect Your Account</p>
                <p style='margin: 0; color: #004085; font-size: 13px; line-height: 1.6;'>
                    <strong>While you have access, we strongly recommend:</strong><br><br>
                    • <strong>Keep your authenticator app safe</strong> – Back up your authenticator app or save the recovery key<br>
                    • <strong>Always have backup codes</strong> – Generate a new set when you have 3 or fewer remaining<br>
                    • <strong>Update your recovery email</strong> – Ensure your backup email is current<br>
                    • <strong>Enable additional security</strong> – Consider using hardware security keys
                </p>
            </div>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#dc3545", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task Send2FADisabledEmailAsync(string toEmail, string adminName, string ipAddress, string? language = null)
    {
        _localizationHelper.SetCulture(language);
        
        var subject = "🔓 SYNFLOX Two-Factor Authentication Disabled";
        var title = "2FA Disabled on Your Account";
        
        var message = $@"Hi {adminName},
            <br><br>
            This email confirms that <strong>Two-Factor Authentication (2FA) has been disabled</strong> on your SYNFLOX administrator account.
            <br><br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Security Alert: 2FA Disabled</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    Your account security has been reduced. We strongly recommend re-enabling Two-Factor Authentication for maximum protection.
                </p>
            </div>
            <br>
            <div style='background-color: #f8f9fa; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 0 0 8px 0; color: #4a5568; font-size: 13px;'><strong>Change Details:</strong></p>
                <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                    <strong>Time:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                    <strong>IP Address:</strong> {ipAddress}<br>
                    <strong>Account:</strong> {toEmail}<br>
                    <strong>Action:</strong> All backup codes deleted, 2FA secret removed
                </p>
            </div>
            <br>
            <div style='background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #721c24; font-size: 14px; font-weight: 600;'>🚨 Didn't Disable 2FA?</p>
                <p style='margin: 0; color: #721c24; font-size: 13px; line-height: 1.6;'>
                    If you didn't disable Two-Factor Authentication, <strong>someone else has accessed your account.</strong> Take these steps immediately:<br><br>
                    <strong>1.</strong> Change your password right away<br>
                    <strong>2.</strong> Enable Two-Factor Authentication again<br>
                    <strong>3.</strong> Contact our support team immediately<br>
                    <strong>4.</strong> Review all recent account activity
                </p>
            </div>
            <br>
            <div style='background-color: #e7f3ff; border-left: 4px solid #0066cc; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #004085; font-size: 14px; font-weight: 600;'>🔐 Re-Enable 2FA for Better Security</p>
                <p style='margin: 0; color: #004085; font-size: 13px; line-height: 1.6;'>
                    <strong>To re-enable Two-Factor Authentication:</strong><br><br>
                    <strong>1.</strong> Sign in to your SYNFLOX account<br>
                    <strong>2.</strong> Go to Profile → Security Settings<br>
                    <strong>3.</strong> Click &quot;Enable Two-Factor Authentication&quot;<br>
                    <strong>4.</strong> Scan the QR code with your authenticator app<br>
                    <strong>5.</strong> Generate and save new backup codes
                </p>
            </div>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#ffc107", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task Send2FAResetEmailAsync(string toEmail, string adminName, string ipAddress, string? language = null)
    {
        _localizationHelper.SetCulture(language);
        
        var subject = "🔄 SYNFLOX Two-Factor Authentication Reset";
        var title = "2FA Reset on Your Account";
        
        var message = $@"Hi {adminName},
            <br><br>
            This email confirms that <strong>Two-Factor Authentication (2FA) has been reset</strong> on your SYNFLOX administrator account.
            <br><br>
            <div style='background-color: #e7f3ff; border-left: 4px solid #0066cc; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #004085; font-size: 14px; font-weight: 600;'>🔄 2FA Has Been Reset</p>
                <p style='margin: 0; color: #004085; font-size: 13px; line-height: 1.6;'>
                    A new 2FA secret has been generated for your account. Your old authenticator codes will no longer work, and all backup codes have been deleted.
                </p>
            </div>
            <br>
            <div style='background-color: #f8f9fa; border-radius: 6px; padding: 16px; margin: 20px 0;'>
                <p style='margin: 0 0 8px 0; color: #4a5568; font-size: 13px;'><strong>Reset Details:</strong></p>
                <p style='margin: 0; color: #6c757d; font-size: 12px; line-height: 1.6;'>
                    <strong>Time:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                    <strong>IP Address:</strong> {ipAddress}<br>
                    <strong>Account:</strong> {toEmail}<br>
                    <strong>Action:</strong> New secret generated, old backup codes deleted
                </p>
            </div>
            <br>
            <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #856404; font-size: 14px; font-weight: 600;'>⚠️ Action Required</p>
                <p style='margin: 0; color: #856404; font-size: 13px; line-height: 1.6;'>
                    <strong>You must complete these steps to finish the 2FA reset:</strong><br><br>
                    <strong>1.</strong> Scan the new QR code with your authenticator app<br>
                    <strong>2.</strong> Verify the 2FA code works<br>
                    <strong>3.</strong> Generate new backup codes<br>
                    <strong>4.</strong> Store your new backup codes securely
                </p>
            </div>
            <br>
            <div style='background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 16px 20px; border-radius: 4px; margin: 24px 0;'>
                <p style='margin: 0 0 12px 0; color: #721c24; font-size: 14px; font-weight: 600;'>🚨 Didn't Reset Your 2FA?</p>
                <p style='margin: 0; color: #721c24; font-size: 13px; line-height: 1.6;'>
                    If you didn't request a 2FA reset, <strong>someone else may have access to your account.</strong> Take immediate action:<br><br>
                    <strong>1.</strong> Change your password immediately<br>
                    <strong>2.</strong> Reset your 2FA again using a device you trust<br>
                    <strong>3.</strong> Contact our support team urgently<br>
                    <strong>4.</strong> Review all recent account activity
                </p>
            </div>
            <br>
            <p style='margin: 0; color: #6c757d; font-size: 13px; line-height: 1.6;'>
                Best regards,<br>
                <strong>The SYNFLOX Security Team</strong>
            </p>";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#0066cc", language);
        await SendEmailAsync(toEmail, subject, body);
    }

    // ===== CUSTOM EMAIL METHODS =====

    public async Task<CustomEmailResponse> SendCustomEmailAsync(CustomEmailRequest request, string? language = null)
    {
        try
        {
            // Set culture for localization using query parameter
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetCustomEmailContent();

            // Decrypt the Company ID before querying database using AutoMapper
            var decryptedCompanyId = _mapper.Map<Guid>(request);
            
            // Fetch company details from database using decrypted CompanyId
            var company = await _companyRepository.GetByIdAsync(decryptedCompanyId, null);
            if (company == null)
            {
                throw new ArgumentException($"Company with ID {decryptedCompanyId} not found");
            }
            
            var toEmail = company.ContactEmail;
            var companyName = company.Name;

            // Build custom message with optional reason and notes (only if provided)
            var customMessage = request.Message;
            
            // Only add reason section if reason is provided and not empty
            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                customMessage += $@"<br><br>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                        <strong>📝 {content.ReasonLabel}:</strong> {request.Reason}
                    </div>";
            }

            // Only add notes section if notes are provided and not empty
            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                customMessage += $@"<br><br>
                    <div style='background-color: #e3f2fd; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                        <strong>📋 {content.NotesLabel}:</strong> {request.Notes}
                    </div>";
            }

            // Choose template based on user preference
            string body;
            if (request.UseMinimalTemplate)
            {
                // Use minimal template with only content (no SYNFLOX branding)
                body = BuildMinimalEmailTemplate(
                    request.Title, 
                    customMessage, 
                    request.AccentColor,
                    null, // request.ButtonText,
                    null, // request.ButtonUrl,
                    null, // request.CustomStyles,
                    language);
            }
            else
            {
                // Use full SYNFLOX branded template with customizable colors
                body = BuildCustomLocalizedEmailTemplate(
                    companyName, 
                    request.Title, 
                    customMessage, 
                    request.AccentColor, 
                    language,
                    null, // request.CustomHeader,
                    null, // request.CustomFooter,
                    null, // request.ButtonText,
                    null, // request.ButtonUrl,
                    null, // request.CustomStyles,
                    request.HeaderColor,
                    request.FooterColor,
                    request.BodyColor,
                    request.TextColor);
            }

            await SendEmailAsync(toEmail, request.Subject, body);

            return new CustomEmailResponse
            {
                Success = true,
                Message = "Email sent successfully",
                SentAt = DateTime.UtcNow,
                RecipientEmail = toEmail,
                CompanyName = companyName
            };
        }
        catch (Exception ex)
        {
            return new CustomEmailResponse
            {
                Success = false,
                Message = "Failed to send email",
                ErrorDetails = ex.Message,
                SentAt = DateTime.UtcNow
            };
        }
    }

    public async Task<BulkCustomEmailResponse> SendBulkCustomEmailAsync(BulkCustomEmailRequest request, string? language = null)
    {
        var response = new BulkCustomEmailResponse
        {
            TotalEmails = request.CompanyIds.Count
        };

        // Send to companies by ID
        foreach (var companyId in request.CompanyIds)
        {
            try
            {
                // Create individual request with company ID (email will be fetched automatically)
                var individualRequest = new CustomEmailRequest
                {
                    CompanyId = companyId,
                    Subject = request.Subject,
                    Title = request.Title,
                    Message = request.Message,
                    Reason = request.Reason,
                    Priority = request.Priority,
                    AccentColor = request.AccentColor,
                    HeaderColor = request.HeaderColor,
                    FooterColor = request.FooterColor,
                    BodyColor = request.BodyColor,
                    TextColor = request.TextColor,
                    IncludeBranding = request.IncludeBranding,
                    IncludeFooter = request.IncludeFooter,
                    UseMinimalTemplate = request.UseMinimalTemplate
                };

                var result = await SendCustomEmailAsync(individualRequest, language);
                response.Results.Add(result);

                if (result.Success)
                    response.SuccessfulEmails++;
                else
                    response.FailedEmails++;
            }
            catch (Exception ex)
            {
                response.Results.Add(new CustomEmailResponse
                {
                    Success = false,
                    Message = "Failed to send email",
                    ErrorDetails = ex.Message,
                    SentAt = DateTime.UtcNow
                });
                response.FailedEmails++;
            }
        }


        return response;
    }

    /// <summary>
    /// Build minimal email template with only content (no SYNFLOX branding)
    /// Perfect for clean, simple emails with just your message
    /// </summary>
    private string BuildMinimalEmailTemplate(
        string title,
        string message,
        string accentColor,
        string? buttonText = null,
        string? buttonUrl = null,
        string? customStyles = null,
        string? language = null)
    {
        // Build call-to-action button if provided
        var buttonSection = !string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(buttonUrl)
            ? $@"<div style='text-align: center; margin: 30px 0;'>
                    <a href='{buttonUrl}' style='background: {accentColor}; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: 600; display: inline-block; box-shadow: 0 4px 12px rgba(0,0,0,0.15); transition: all 0.3s ease;'>
                        {buttonText}
                    </a>
                 </div>"
            : "";

        // RTL styles for Arabic
        var rtlStyles = language == "ar" ? @"
        body { direction: rtl; text-align: right; }
        .container { direction: rtl; text-align: right; }
        .content { direction: rtl; text-align: right; }
        .title { direction: rtl; text-align: center; }
        " : "";

        return $@"<!DOCTYPE html>
<html lang='{(language == "ar" ? "ar" : "en")}' dir='{(language == "ar" ? "rtl" : "ltr")}'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <style>
        body {{ font-family: {(language == "ar" ? "'Segoe UI', 'Tahoma', 'Arial', sans-serif" : "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif")}; margin: 0; padding: 20px; background-color: #f5f7fa; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1); }}
        .title {{ font-size: 24px; font-weight: 700; margin: 0 0 20px 0; color: #2c3e50; text-align: center; }}
        .content {{ font-size: 16px; line-height: 1.6; color: #2c3e50; }}
        .divider {{ height: 2px; background: {accentColor}; margin: 20px 0; border-radius: 1px; }}
        {rtlStyles}
        {customStyles ?? ""}
    </style>
</head>
<body>
    <div class='container'>
        <h1 class='title'>{title}</h1>
        <div class='divider'></div>
        <div class='content'>
            {message}
        </div>
        {buttonSection}
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Build fully customizable localized email template for custom emails
    /// Maintains SYNFLOX identity while allowing content customization
    /// </summary>
    private string BuildCustomLocalizedEmailTemplate(
        string companyName, 
        string title, 
        string message, 
        string accentColor, 
        string? language = null,
        string? customHeader = null,
        string? customFooter = null,
        string? buttonText = null,
        string? buttonUrl = null,
        string? customStyles = null,
        string? headerColor = null,
        string? footerColor = null,
        string? bodyColor = null,
        string? textColor = null)
    {
        _localizationHelper.SetCulture(language);
        var commonContent = _localizationHelper.GetCommonContent();
        
        // Use fallback colors if not specified
        var effectiveHeaderColor = headerColor ?? accentColor;
        var effectiveFooterColor = footerColor ?? accentColor;
        var effectiveBodyColor = bodyColor ?? "#ffffff";
        var effectiveTextColor = textColor ?? "#2c3e50";
        
        var logoUrl = GetLogoUrl();
        var logoStyle = GetLogoStyle();
        var currentYear = DateTime.UtcNow.Year;
        
        // Build custom announcement section if provided
        var announcementSection = !string.IsNullOrEmpty(customHeader) 
            ? $@"<div style='background: linear-gradient(135deg, {accentColor}22, {accentColor}11); border: 2px solid {accentColor}; color: #2c3e50; padding: 20px; text-align: center; border-radius: 12px; margin: 20px 0;'>
                    <h2 style='margin: 0; font-size: 18px; font-weight: 600; color: {accentColor};'>📢 {customHeader}</h2>
                 </div>" 
            : "";

        // Build call-to-action button if provided
        var buttonSection = !string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(buttonUrl)
            ? $@"<div style='text-align: center; margin: 30px 0;'>
                    <a href='{buttonUrl}' style='background: {accentColor}; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; font-weight: 600; display: inline-block; box-shadow: 0 4px 12px rgba(0,0,0,0.15); transition: all 0.3s ease;'>
                        {buttonText}
                    </a>
                 </div>"
            : "";

        // Build custom additional info section if provided
        var additionalInfoSection = !string.IsNullOrEmpty(customFooter)
            ? $@"<div style='background: linear-gradient(135deg, #f8f9fa, #e9ecef); padding: 20px; margin: 20px 0; border-radius: 12px; border-left: 4px solid {accentColor};'>
                    <h4 style='margin: 0 0 10px 0; color: {accentColor}; font-size: 16px;'>ℹ️ Additional Information</h4>
                    <p style='margin: 0; color: #495057; font-size: 14px; line-height: 1.5;'>{customFooter}</p>
                 </div>"
            : "";

        // RTL styles for Arabic
        var rtlStyles = language == "ar" ? @"
        body { direction: rtl; text-align: right; }
        .container { direction: rtl; }
        .content { direction: rtl; text-align: right; }
        .header { direction: rtl; text-align: center; }
        .footer { direction: rtl; text-align: center; }
        .greeting { direction: rtl; text-align: right; }
        .message-content { direction: rtl; text-align: right; }
        .support-section { direction: rtl; text-align: center; }
        .company-info { direction: rtl; text-align: right; }
        " : "";

        return $@"<!DOCTYPE html>
<html lang='{(language == "ar" ? "ar" : "en")}' dir='{(language == "ar" ? "rtl" : "ltr")}'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <style>
        body {{ font-family: {(language == "ar" ? "'Segoe UI', 'Tahoma', 'Arial', sans-serif" : "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif")}; margin: 0; padding: 0; background-color: #f5f7fa; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: {effectiveBodyColor}; box-shadow: 0 8px 32px rgba(0,0,0,0.1); border-radius: 16px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, {effectiveHeaderColor}, {effectiveHeaderColor}dd); color: white; padding: 40px 30px; text-align: center; }}
        .content {{ padding: 40px 30px; background-color: {effectiveBodyColor}; }}
        .footer {{ background: linear-gradient(135deg, {effectiveFooterColor}, {effectiveFooterColor}dd); color: white; padding: 30px; text-align: center; }}
        .logo {{ {logoStyle} }}
        {rtlStyles}
        .title {{ font-size: 28px; font-weight: 700; margin: 0; text-shadow: 0 2px 4px rgba(0,0,0,0.3); }}
        .message {{ font-size: 16px; line-height: 1.6; color: {effectiveTextColor}; margin: 20px 0; }}
        .company-name {{ color: {accentColor}; font-weight: 600; }}
        .divider {{ height: 3px; background: linear-gradient(90deg, {accentColor}, transparent); margin: 30px 0; border-radius: 2px; }}
        .synflox-brand {{ background: linear-gradient(135deg, #667eea, #764ba2); -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; font-weight: 800; }}
        {customStyles ?? ""}
    </style>
</head>
<body>
    <div class='container'>
        <!-- SYNFLOX Header - Always Present (Our Identity) -->
        <div class='header'>
            {(string.IsNullOrEmpty(logoUrl) ? "" : $"<img src='{logoUrl}' alt='SYNFLOX' class='logo'>")}
            <h1 class='title'>{title}</h1>
            <p style='margin: 10px 0 0 0; opacity: 0.9; font-size: 14px;'>Central Licensing System</p>
        </div>
        
        <div class='content'>
            <!-- Custom Announcement Section (Customizable) -->
            {announcementSection}
            
            <!-- Company Greeting (Fixed: Company Name) -->
            <p class='greeting' style='font-size: 18px; color: #2c3e50; margin-bottom: 10px;'>
                {commonContent.HelloLabel} <span class='company-name'>{companyName}</span>! 👋
            </p>
            
            <div class='divider'></div>
            
            <!-- Main Message Content (Fully Customizable) -->
            <div class='message message-content'>
                {message}
            </div>
            
            <!-- Call-to-Action Button (Customizable) -->
            {buttonSection}
            
            <!-- Additional Information Section (Customizable) -->
            {additionalInfoSection}
            
            <!-- SYNFLOX Support Section - Always Present (Our Identity) -->
            <div class='support-section' style='margin-top: 40px; padding: 20px; background: linear-gradient(135deg, #f8f9fa, #e9ecef); border-radius: 12px; border: 1px solid #dee2e6;'>
                <h3 style='color: {accentColor}; margin: 0 0 15px 0; font-size: 16px;'>📞 {commonContent.NeedHelpLabel}</h3>
                <p style='margin: 0; color: #6c757d; font-size: 14px;'>
                    {commonContent.SupportMessage}<br>
                    📧 support@synflox.com | 📱 +966-11-123-4567 | 🌐 www.synflox.com
                </p>
            </div>
        </div>
        
        <!-- SYNFLOX Footer - Always Present (Our Identity) -->
        <div class='footer'>
            <div style='margin-bottom: 15px;'>
                <span class='synflox-brand' style='font-size: 20px;'>SYNFLOX</span>
                <p style='margin: 5px 0 0 0; font-size: 12px; opacity: 0.8;'>Central Licensing System</p>
            </div>
            <p style='margin: 0 0 10px 0; font-size: 14px; opacity: 0.9;'>
                {string.Format(commonContent.CopyrightText, currentYear)}
            </p>
            <p style='margin: 0; font-size: 12px; opacity: 0.7;'>
                {commonContent.PoweredByText}
            </p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Build simple email template without full branding
    /// </summary>
    private string BuildSimpleEmailTemplate(string companyName, string title, string message, string accentColor)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f8fafc; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.05);"">
                    <tr>
                        <td style=""background: {accentColor}; padding: 30px; text-align: center;"">
                            <h1 style=""color: white; margin: 0; font-size: 24px; font-weight: 600;"">{title}</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px;"">
                            <p style=""color: #4a5568; line-height: 1.7; margin: 0 0 20px 0; font-size: 16px;"">Hello <strong style=""color: #2d3748;"">{companyName}</strong>,</p>
                            <div style=""color: #4a5568; line-height: 1.7; margin: 0; font-size: 16px;"">{message}</div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    // ============================================================================
    // SUBSCRIPTION LIFECYCLE EMAIL METHODS
    // ============================================================================

    public async Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, string? language = null)
    {
        try
        {
            var subject = $"Subscription Created - {companyName}";
            var message = $"Your subscription to <strong>{planName}</strong> has been successfully created and is now active.<br><br>Expiry Date: <strong>{expiryDate:MMMM dd, yyyy}</strong>";
            var htmlBody = BuildCustomLocalizedEmailTemplate(companyName, subject, message, "#4CAF50", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription created email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? reason = null, string? language = null)
    {
        try
        {
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetSubscriptionRenewedContent();
            var isRtl = _localizationHelper.IsRtl();
            
            var subject = string.Format(content.Subject, companyName);
            var expiryFormat = isRtl ? newExpiryDate.ToString("yyyy/MM/dd") : newExpiryDate.ToString("MMMM dd, yyyy");
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><strong>{content.ReasonLabel}:</strong> {reason}" : "";
            
            var message = $@"{content.Description}
                <br><br>
                <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
                <strong>📅 {content.ExpiryDateLabel}:</strong> {expiryFormat}
                {reasonText}";
            
            var htmlBody = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription renewed email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlanName, string newPlanName, DateTime expiryDate, string? reason = null, string? language = null)
    {
        try
        {
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetSubscriptionUpgradedContent();
            var isRtl = _localizationHelper.IsRtl();
            
            var subject = string.Format(content.Subject, companyName);
            var expiryFormat = isRtl ? expiryDate.ToString("yyyy/MM/dd") : expiryDate.ToString("MMMM dd, yyyy");
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><strong>{content.ReasonLabel}:</strong> {reason}" : "";
            
            var message = $@"{content.Description}
                <br><br>
                <strong>📦 {content.PlanLabel} (Old):</strong> {oldPlanName}<br>
                <strong>📦 {content.PlanLabel} (New):</strong> {newPlanName}<br>
                <strong>📅 {content.ExpiryDateLabel}:</strong> {expiryFormat}
                {reasonText}";
            
            var htmlBody = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#2196F3", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription upgraded email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionExtendedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, int extensionDays, string? reason = null, string? language = null)
    {
        try
        {
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetSubscriptionExtendedContent();
            var isRtl = _localizationHelper.IsRtl();
            
            var subject = string.Format(content.Subject, companyName);
            var expiryFormat = isRtl ? newExpiryDate.ToString("yyyy/MM/dd") : newExpiryDate.ToString("MMMM dd, yyyy");
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><strong>{content.ReasonLabel}:</strong> {reason}" : "";
            
            var message = $@"{content.Description}
                <br><br>
                <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
                <strong>📅 {content.ExpiryDateLabel}:</strong> {expiryFormat}<br>
                <strong>📆 {content.ExtensionPeriodLabel}:</strong> {extensionDays} {content.DaysLabel}
                {reasonText}";
            
            var htmlBody = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#00BCD4", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription extended email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionCancelledEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null)
    {
        try
        {
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetSubscriptionCanceledContent();
            var isRtl = _localizationHelper.IsRtl();
            
            var subject = string.Format(content.Subject, companyName);
            var dateFormat = isRtl ? DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm") : DateTime.UtcNow.ToString("MMMM dd, yyyy HH:mm");
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><strong>{content.ReasonLabel}:</strong> {reason}" : "";
            
            var message = $@"{content.Description}
                <br><br>
                <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
                <strong>📅 {content.DateLabel}:</strong> {dateFormat} UTC
                {reasonText}";
            
            var htmlBody = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#F44336", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription cancelled email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null)
    {
        try
        {
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetSubscriptionSuspendedContent();
            var isRtl = _localizationHelper.IsRtl();
            
            var subject = string.Format(content.Subject, companyName);
            var dateFormat = isRtl ? DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm") : DateTime.UtcNow.ToString("MMMM dd, yyyy HH:mm");
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><strong>{content.ReasonLabel}:</strong> {reason}" : "";
            
            var message = $@"{content.Description}
                <br><br>
                <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
                <strong>📅 {content.DateLabel}:</strong> {dateFormat} UTC
                {reasonText}";
            
            var htmlBody = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#FF9800", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription suspended email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionResumedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null)
    {
        try
        {
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetSubscriptionResumedContent();
            var isRtl = _localizationHelper.IsRtl();
            
            var subject = string.Format(content.Subject, companyName);
            var dateFormat = isRtl ? DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm") : DateTime.UtcNow.ToString("MMMM dd, yyyy HH:mm");
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><strong>{content.ReasonLabel}:</strong> {reason}" : "";
            
            var message = $@"{content.Description}
                <br><br>
                <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
                <strong>📅 {content.DateLabel}:</strong> {dateFormat} UTC
                {reasonText}";
            
            var htmlBody = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#4CAF50", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription resumed email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionPausedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null)
    {
        try
        {
            _localizationHelper.SetCulture(language);
            var content = _localizationHelper.GetSubscriptionPausedContent();
            var isRtl = _localizationHelper.IsRtl();
            
            var subject = string.Format(content.Subject, companyName);
            var dateFormat = isRtl ? DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm") : DateTime.UtcNow.ToString("MMMM dd, yyyy HH:mm");
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><strong>{content.ReasonLabel}:</strong> {reason}" : "";
            
            var message = $@"{content.Description}
                <br><br>
                <strong>📦 {content.PlanLabel}:</strong> {planName}<br>
                <strong>📅 {content.DateLabel}:</strong> {dateFormat} UTC
                {reasonText}";
            
            var htmlBody = BuildSubscriptionActionEmailTemplate(companyName, content.Title, message, "#9C27B0", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription paused email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionUnpausedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null)
    {
        try
        {
            var subject = $"Subscription Unpaused - {companyName}";
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><br><strong>Reason:</strong> {reason}" : "";
            var message = $"Your subscription to <strong>{planName}</strong> has been unpaused and is now active.{reasonText}";
            var htmlBody = BuildCustomLocalizedEmailTemplate(companyName, subject, message, "#4CAF50", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription unpaused email: {ex.Message}");
        }
    }

    public async Task SendSubscriptionReactivatedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null)
    {
        try
        {
            var subject = $"Subscription Reactivated - {companyName}";
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><br><strong>Reason:</strong> {reason}" : "";
            var message = $"Your subscription to <strong>{planName}</strong> has been reactivated and is now active.{reasonText}";
            var htmlBody = BuildCustomLocalizedEmailTemplate(companyName, subject, message, "#4CAF50", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send subscription reactivated email: {ex.Message}");
        }
    }

    public async Task SendTrialStoppedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null)
    {
        try
        {
            var subject = $"Trial Converted to Paid - {companyName}";
            var reasonText = !string.IsNullOrEmpty(reason) ? $"<br><br><strong>Reason:</strong> {reason}" : "";
            var message = $"Your trial subscription to <strong>{planName}</strong> has been converted to a paid subscription.{reasonText}";
            var htmlBody = BuildCustomLocalizedEmailTemplate(companyName, subject, message, "#607D8B", language);
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send trial stopped email: {ex.Message}");
        }
    }
}