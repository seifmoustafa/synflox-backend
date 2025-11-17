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
    }

    public async Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, bool isTrial, string? language = null)
    {
        var subject = isTrial 
            ? $"🎉 Your SYNFLOX Trial Has Started - {companyName}" 
            : $"✅ Your SYNFLOX Subscription is Active - {companyName}";
        
        var title = isTrial ? "🎉 Trial Started Successfully!" : "✅ Subscription Activated!";
        
        var message = isTrial 
            ? $@"Congratulations! Your <strong>{planName}</strong> trial subscription has been successfully activated.
                <br><br>
                <strong>📅 Trial Started:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                <strong>⏰ Trial Expires:</strong> {expiryDate:MMMM dd, yyyy} at {expiryDate:HH:mm} UTC<br>
                <strong>📦 Plan:</strong> {planName}<br>
                <strong>🏢 Company:</strong> {companyName}<br>
                <strong>⏱️ Trial Duration:</strong> {(expiryDate - DateTime.UtcNow).Days} days
                <br><br>
                <strong>🚀 What's included in your trial:</strong><br>
                • Full access to all {planName} features<br>
                • Complete licensing functionality<br>
                • Technical support during trial period<br>
                • No limitations or restrictions
                <br><br>
                Make the most of your trial period and experience the full power of SYNFLOX!"
            : $@"Congratulations! Your <strong>{planName}</strong> subscription has been successfully activated.
                <br><br>
                <strong>📅 Activation Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
                <strong>⏰ Expires On:</strong> {expiryDate:MMMM dd, yyyy} at {expiryDate:HH:mm} UTC<br>
                <strong>📦 Plan:</strong> {planName}<br>
                <strong>🏢 Company:</strong> {companyName}<br>
                <strong>⏱️ Subscription Duration:</strong> {(expiryDate - DateTime.UtcNow).Days} days
                <br><br>
                <strong>✨ Your subscription includes:</strong><br>
                • Complete access to all {planName} features<br>
                • Advanced licensing management<br>
                • Priority technical support<br>
                • Regular updates and improvements<br>
                • Secure cloud-based infrastructure
                <br><br>
                Your subscription is now active and you have full access to all features.";
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionActivatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, string? language = null)
    {
        var subject = $"✅ SYNFLOX Subscription Activated - {companyName}";
        var title = "✅ Subscription Successfully Activated!";
        
        var message = $@"Great news! Your <strong>{planName}</strong> subscription has been successfully activated.
            <br><br>
            <strong>📅 Activation Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ Expires On:</strong> {expiryDate:MMMM dd, yyyy} at {expiryDate:HH:mm} UTC<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>⏱️ Duration:</strong> {(expiryDate - DateTime.UtcNow).Days} days
            <br><br>
            <strong>✨ Your subscription includes:</strong><br>
            • Complete access to all {planName} features<br>
            • Advanced licensing management tools<br>
            • Priority technical support<br>
            • Regular updates and improvements<br>
            • Secure cloud-based infrastructure<br>
            • Comprehensive analytics and reporting
            <br><br>
            Your subscription is now fully active and ready to use!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionExpiredEmailAsync(string toEmail, string companyName, string planName, string? language = null)
    {
        var subject = $"⚠️ SYNFLOX Subscription Expired - {companyName}";
        var title = "⚠️ Subscription Has Expired";
        
        var message = $@"Your <strong>{planName}</strong> subscription has expired and requires renewal.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Expiration Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC
            <br><br>
            <strong>⚠️ What this means:</strong><br>
            • Your access to SYNFLOX services has been suspended<br>
            • Your data remains safe and secure<br>
            • You need to renew your subscription to continue using our services<br>
            • Contact support for renewal options
            <br><br>
            <strong>🔄 Next Steps:</strong><br>
            • Contact our sales team for renewal options<br>
            • Choose from our available subscription plans<br>
            • Restore full access to all features
            <br><br>
            Don't let your business operations be interrupted - renew today!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#F44336");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string reason, string? language = null)
    {
        var subject = $"⚠️ SYNFLOX Subscription Suspended - {companyName}";
        var title = "⚠️ Subscription Suspended";
        
        var message = $@"Your SYNFLOX subscription has been temporarily suspended.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📅 Suspension Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📝 Reason:</strong> {reason}
            <br><br>
            <strong>What this means:</strong><br>
            • Your access to SYNFLOX services is temporarily restricted<br>
            • Your data remains safe and secure<br>
            • Contact support to resolve this issue and reactivate your subscription
            <br><br>
            Please contact our support team immediately to resolve this matter.";
        
        var body = BuildEmailTemplate(companyName, title, message, "#FF9800");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null)
    {
        var subject = $"🔄 SYNFLOX Subscription Renewed - {companyName}";
        var title = "🔄 Subscription Successfully Renewed!";
        
        var message = $@"Great news! Your SYNFLOX subscription has been successfully renewed.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Renewal Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ New Expiry Date:</strong> {newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm} UTC
            <br><br>
            <strong>What's included:</strong><br>
            • Continued access to all {planName} features<br>
            • Uninterrupted service until {newExpiryDate:MMMM dd, yyyy}<br>
            • Full technical support<br>
            • All future updates and improvements
            <br><br>
            Thank you for continuing to trust SYNFLOX for your licensing needs!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlan, string newPlan, DateTime newExpiryDate, string? language = null)
    {
        var subject = $"🚀 SYNFLOX Subscription Upgraded - {companyName}";
        var title = "🚀 Subscription Successfully Upgraded!";
        
        var message = $@"Excellent! Your SYNFLOX subscription has been successfully upgraded.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📈 Upgrade:</strong> {oldPlan} → {newPlan}<br>
            <strong>📅 Upgrade Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ New Expiry Date:</strong> {newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm} UTC<br>
            <strong>⏱️ Duration:</strong> {(newExpiryDate - DateTime.UtcNow).Days} days
            <br><br>
            <strong>✨ Your upgraded subscription includes:</strong><br>
            • All enhanced features of the {newPlan} plan<br>
            • Improved performance and capabilities<br>
            • Priority technical support<br>
            • Advanced analytics and reporting<br>
            • Extended functionality and integrations<br>
            • Secure cloud-based infrastructure
            <br><br>
            Enjoy your enhanced SYNFLOX experience with {newPlan}!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#9C27B0");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionCanceledEmailAsync(string toEmail, string companyName, string planName, string? language = null)
    {
        var subject = $"❌ SYNFLOX Subscription Canceled - {companyName}";
        var title = "❌ Subscription Canceled";
        
        var message = $@"Your SYNFLOX subscription has been canceled as requested.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Cancellation Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC
            <br><br>
            <strong>📝 What this means:</strong><br>
            • Your subscription has been immediately terminated<br>
            • Access to SYNFLOX services has been revoked<br>
            • Your data will be retained for 30 days for recovery<br>
            • You can reactivate anytime by contacting support
            <br><br>
            <strong>🔄 Want to come back?</strong><br>
            • Contact our support team for reactivation<br>
            • Choose from our available subscription plans<br>
            • Your data can be restored within 30 days
            <br><br>
            We're sorry to see you go. Thank you for using SYNFLOX!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#607D8B");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialStartedEmailAsync(string toEmail, string companyName, string planName, int trialDays, DateTime expiryDate, string? language = null)
    {
        var subject = $"🎉 Your SYNFLOX Trial Has Started - {companyName}";
        var title = "🎉 Trial Started Successfully!";
        
        var message = $@"Welcome to SYNFLOX! Your <strong>{planName}</strong> trial has been successfully activated.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Trial Plan:</strong> {planName}<br>
            <strong>📅 Trial Started:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ Trial Expires:</strong> {expiryDate:MMMM dd, yyyy} at {expiryDate:HH:mm} UTC<br>
            <strong>⏱️ Trial Duration:</strong> {trialDays} days
            <br><br>
            <strong>🚀 What's included in your trial:</strong><br>
            • Full access to all {planName} features<br>
            • Complete licensing functionality<br>
            • Advanced analytics and reporting<br>
            • Technical support during trial period<br>
            • No limitations or restrictions<br>
            • Secure cloud-based infrastructure
            <br><br>
            <strong>💡 Make the most of your trial:</strong><br>
            • Explore all available features<br>
            • Test integration with your systems<br>
            • Contact support for any questions<br>
            • Consider upgrading before trial expires
            <br><br>
            Experience the full power of SYNFLOX during your {trialDays}-day trial!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#00BCD4");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialExpiringEmailAsync(string toEmail, string companyName, string planName, int daysRemaining, string? language = null)
    {
        var subject = $"⚠️ SYNFLOX Trial Expiring Soon - {companyName}";
        var title = $"⚠️ Trial Expires in {daysRemaining} Days!";
        
        var message = $@"Your SYNFLOX <strong>{planName}</strong> trial is expiring soon!
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Trial Plan:</strong> {planName}<br>
            <strong>⏰ Days Remaining:</strong> {daysRemaining} days<br>
            <strong>📅 Expiration Date:</strong> {DateTime.UtcNow.AddDays(daysRemaining):MMMM dd, yyyy}
            <br><br>
            <strong>🚀 Don't lose access to:</strong><br>
            • All {planName} premium features<br>
            • Advanced licensing management<br>
            • Analytics and reporting tools<br>
            • Priority technical support<br>
            • Secure cloud infrastructure
            <br><br>
            <strong>💳 Upgrade now to continue enjoying:</strong><br>
            • Uninterrupted service<br>
            • All premium features<br>
            • Priority support<br>
            • Regular updates and improvements
            <br><br>
            <strong>📧 Contact our sales team today to upgrade your subscription!</strong>";
        
        var body = BuildEmailTemplate(companyName, title, message, "#FF5722");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendAutoRenewalEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null)
    {
        var subject = $"🔄 SYNFLOX Auto-Renewal Successful - {companyName}";
        var title = "🔄 Subscription Auto-Renewed!";
        
        var message = $@"Great news! Your SYNFLOX subscription has been automatically renewed.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Renewal Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ New Expiry Date:</strong> {newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm} UTC<br>
            <strong>⏱️ Extended Duration:</strong> {(newExpiryDate - DateTime.UtcNow).Days} days
            <br><br>
            <strong>✨ Your renewed subscription includes:</strong><br>
            • Continued access to all {planName} features<br>
            • Uninterrupted service until {newExpiryDate:MMMM dd, yyyy}<br>
            • Priority technical support<br>
            • All future updates and improvements<br>
            • Secure cloud-based infrastructure
            <br><br>
            Thank you for your continued trust in SYNFLOX!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionPausedEmailAsync(string toEmail, string companyName, string planName, string reason, string? language = null)
    {
        var subject = $"⏸️ SYNFLOX Subscription Paused - {companyName}";
        var title = "⏸️ Subscription Temporarily Paused";
        
        var message = $@"Your SYNFLOX subscription has been temporarily paused.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Pause Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📝 Reason:</strong> {reason}
            <br><br>
            <strong>What this means:</strong><br>
            • Your subscription timer is paused<br>
            • No billing will occur during pause period<br>
            • Your data remains safe and secure<br>
            • You can resume anytime by contacting support
            <br><br>
            Contact our support team when you're ready to resume your subscription.";
        
        var body = BuildEmailTemplate(companyName, title, message, "#FF9800");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionResumedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null)
    {
        var subject = $"▶️ SYNFLOX Subscription Resumed - {companyName}";
        var title = "▶️ Subscription Successfully Resumed!";
        
        var message = $@"Welcome back! Your SYNFLOX subscription has been successfully resumed.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Resume Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ Current Expiry Date:</strong> {newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm} UTC<br>
            <strong>⏱️ Remaining Time:</strong> {(newExpiryDate - DateTime.UtcNow).Days} days
            <br><br>
            <strong>🚀 You now have full access to:</strong><br>
            • All {planName} features and capabilities<br>
            • Complete licensing functionality<br>
            • Priority technical support<br>
            • Regular updates and improvements<br>
            • Secure cloud-based infrastructure
            <br><br>
            Thank you for choosing SYNFLOX for your licensing needs!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialStoppedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null)
    {
        var subject = $"🎯 SYNFLOX Trial Converted to Paid - {companyName}";
        var title = "🎯 Trial Successfully Converted!";
        
        var message = $@"Congratulations! Your SYNFLOX trial has been converted to a full paid subscription.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Conversion Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ Subscription Expires:</strong> {newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm} UTC<br>
            <strong>⏱️ Full Subscription Duration:</strong> {(newExpiryDate - DateTime.UtcNow).Days} days
            <br><br>
            <strong>🎉 Welcome to the full SYNFLOX experience:</strong><br>
            • Complete access to all {planName} features<br>
            • Advanced licensing management tools<br>
            • Priority technical support<br>
            • Regular updates and new features<br>
            • Enterprise-grade security and reliability
            <br><br>
            Thank you for upgrading to our full subscription!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#9C27B0");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionExtendedEmailAsync(string toEmail, string companyName, string planName, DateTime oldExpiryDate, DateTime newExpiryDate, int extensionDays, string? language = null)
    {
        var subject = $"📅 SYNFLOX Subscription Extended - {companyName}";
        var title = "📅 Subscription Successfully Extended!";
        
        var message = $@"Great news! Your SYNFLOX subscription has been extended.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Extension Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ Previous Expiry:</strong> {oldExpiryDate:MMMM dd, yyyy}<br>
            <strong>⏰ New Expiry Date:</strong> {newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm} UTC<br>
            <strong>⏱️ Extension Period:</strong> {extensionDays} days<br>
            <strong>⏱️ Total Remaining Time:</strong> {(newExpiryDate - DateTime.UtcNow).Days} days
            <br><br>
            <strong>✨ Your extended subscription includes:</strong><br>
            • Continued access to all {planName} features<br>
            • Extended service until {newExpiryDate:MMMM dd, yyyy}<br>
            • Priority technical support<br>
            • All future updates and improvements<br>
            • Secure cloud-based infrastructure
            <br><br>
            Enjoy your extended SYNFLOX experience!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#2196F3");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionReactivatedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null)
    {
        var subject = $"🔄 SYNFLOX Subscription Reactivated - {companyName}";
        var title = "🔄 Subscription Successfully Reactivated!";
        
        var message = $@"Welcome back! Your SYNFLOX subscription has been successfully reactivated.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📦 Plan:</strong> {planName}<br>
            <strong>📅 Reactivation Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>⏰ Subscription Expires:</strong> {newExpiryDate:MMMM dd, yyyy} at {newExpiryDate:HH:mm} UTC<br>
            <strong>⏱️ Subscription Duration:</strong> {(newExpiryDate - DateTime.UtcNow).Days} days
            <br><br>
            <strong>🚀 You now have full access to:</strong><br>
            • All {planName} features and capabilities<br>
            • Complete licensing functionality<br>
            • Priority technical support<br>
            • Regular updates and improvements<br>
            • Secure cloud-based infrastructure
            <br><br>
            We're glad to have you back with SYNFLOX!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
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

    private string BuildEmailTemplate(string companyName, string title, string message, string accentColor)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:5035";
        var logoUrl = $"{baseUrl}/images/app-logo.png";
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
                            <img src=""{logoUrl}"" alt=""SYNFLOX Logo"" style=""max-width: 120px; height: auto; margin-bottom: 20px;"" />
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
                                            📧 Email: <a href=""mailto:{_fromEmail}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_fromEmail}</a><br>
                                            🌐 Website: <a href=""https://synflox.com"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">synflox.com</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            
                            <div style=""margin-top: 40px; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                                <p style=""color: #a0aec0; font-size: 13px; margin: 0; line-height: 1.6; text-align: center;"">
                                    This is an automated notification from SYNFLOX Central Licensing System.<br>
                                    Please do not reply to this email. For support, use the contact information above.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: linear-gradient(90deg, #f7fafc 0%, #edf2f7 100%); padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;"">
                            <p style=""margin: 0 0 10px 0; color: #718096; font-size: 14px; font-weight: 500;"">
                                SYNFLOX - Professional Licensing Solutions
                            </p>
                            <p style=""margin: 0; color: #a0aec0; font-size: 12px;"">
                                &copy; {currentYear} SYNFLOX. All rights reserved. | Powered by Advanced Technology
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
        
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:5035";
        var logoUrl = $"{baseUrl}/images/app-logo.png";
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
                            <img src=""{logoUrl}"" alt=""SYNFLOX Logo"" style=""max-width: 120px; height: auto; margin-bottom: 20px;"" />
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
                                            <p style=""margin: 5px 0;""><strong>{commonContent.SystemLabel}:</strong> SYNFLOX Central Licensing</p>
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
                                            📧 {commonContent.EmailLabel}: <a href=""mailto:{_fromEmail}"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">{_fromEmail}</a><br>
                                            🌐 {commonContent.WebsiteLabel}: <a href=""https://synflox.com"" style=""color: {accentColor}; text-decoration: none; font-weight: 500;"">synflox.com</a>
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
        
        var subject = "🔐 Email Address Changed - SYNFLOX";
        var title = "Email Address Changed";
        
        var message = $@"Hello {adminName},
            <br><br>
            Your email address has been successfully changed.
            <br><br>
            <strong>📧 Previous Email:</strong> {oldEmail}<br>
            <strong>✅ New Email:</strong> {newEmail}<br>
            <strong>📅 Changed At:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC
            <br><br>
            <strong>🔒 Security Notice:</strong><br>
            • This notification has been sent to both your old and new email addresses<br>
            • If you did not make this change, please contact support immediately<br>
            • Your account security may be compromised
            <br><br>
            <strong>🛡️ Next Steps:</strong><br>
            • Verify that you can access your account with the new email<br>
            • Update your email in any third-party systems<br>
            • Consider enabling Two-Factor Authentication for extra security
            <br><br>
            Thank you for keeping your account secure!";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#2196F3", language);
        
        // Send to BOTH old and new email addresses
        await SendEmailAsync(oldEmail, subject, body);
        await SendEmailAsync(newEmail, subject, body);
    }

    public async Task SendPasswordChangedNotificationAsync(string toEmail, string adminName, string? language = null)
    {
        // Set culture for localization
        _localizationHelper.SetCulture(language);
        
        var subject = "🔐 Password Changed - SYNFLOX";
        var title = "Password Changed";
        
        var message = $@"Hello {adminName},
            <br><br>
            Your password has been successfully changed.
            <br><br>
            <strong>📅 Changed At:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📧 Account Email:</strong> {toEmail}
            <br><br>
            <strong>🔒 Security Notice:</strong><br>
            • If you did not make this change, your account may be compromised<br>
            • Contact support immediately if this was unauthorized<br>
            • Consider enabling Two-Factor Authentication
            <br><br>
            <strong>🛡️ Security Tips:</strong><br>
            • Use a strong, unique password for your account<br>
            • Never share your password with anyone<br>
            • Change your password regularly<br>
            • Enable 2FA for maximum security
            <br><br>
            Thank you for keeping your account secure!";
        
        var body = BuildLocalizedEmailTemplate(adminName, title, message, "#FF9800", language);
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
                // Use full SYNFLOX branded template
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
                    null); // request.CustomStyles);
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
        string? customStyles = null)
    {
        _localizationHelper.SetCulture(language);
        var commonContent = _localizationHelper.GetCommonContent();
        
        var logoPath = _configuration["EmailSettings:LogoPath"] ?? "/images/synflox-logo.png";
        var baseUrl = _configuration["EmailSettings:BaseUrl"] ?? "https://synflox.com";
        var currentYear = DateTime.Now.Year;
        
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
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; box-shadow: 0 8px 32px rgba(0,0,0,0.1); border-radius: 16px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #2c3e50, #34495e); color: white; padding: 40px 30px; text-align: center; }}
        .content {{ padding: 40px 30px; }}
        .footer {{ background: linear-gradient(135deg, #2c3e50, #34495e); color: white; padding: 30px; text-align: center; }}
        .logo {{ max-width: 150px; height: auto; margin-bottom: 20px; }}
        {rtlStyles}
        .title {{ font-size: 28px; font-weight: 700; margin: 0; text-shadow: 0 2px 4px rgba(0,0,0,0.3); }}
        .message {{ font-size: 16px; line-height: 1.6; color: #2c3e50; margin: 20px 0; }}
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
            <img src='{baseUrl}{logoPath}' alt='SYNFLOX' class='logo'>
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
}
