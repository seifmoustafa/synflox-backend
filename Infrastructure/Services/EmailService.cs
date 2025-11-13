using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Application.Services;
using Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

/// <summary>
/// Email service using Gmail SMTP with beautiful HTML templates
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILocalizationService _localizer;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _appPassword;

    public EmailService(IConfiguration configuration, ILocalizationService localizer)
    {
        _configuration = configuration;
        _localizer = localizer;
        
        _smtpHost = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
        _fromEmail = _configuration["EmailSettings:FromEmail"] ?? throw new InvalidOperationException("EmailSettings:FromEmail not configured");
        _fromName = _configuration["EmailSettings:FromName"] ?? "SYNFLOX System";
        _appPassword = _configuration["EmailSettings:Pass"] ?? throw new InvalidOperationException("EmailSettings:Pass not configured");
    }

    public async Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, bool isTrial)
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

    public async Task SendSubscriptionActivatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate)
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

    public async Task SendSubscriptionExpiredEmailAsync(string toEmail, string companyName, string planName)
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

    public async Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string reason)
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

    public async Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate)
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

    public async Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlan, string newPlan, DateTime newExpiryDate)
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

    public async Task SendSubscriptionCanceledEmailAsync(string toEmail, string companyName, string planName)
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

    public async Task SendTrialStartedEmailAsync(string toEmail, string companyName, string planName, int trialDays, DateTime expiryDate)
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

    public async Task SendTrialExpiringEmailAsync(string toEmail, string companyName, string planName, int daysRemaining)
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

    public async Task SendAutoRenewalEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate)
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

    public async Task SendSubscriptionPausedEmailAsync(string toEmail, string companyName, string planName, string reason)
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

    public async Task SendSubscriptionResumedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate)
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

    public async Task SendTrialStoppedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate)
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

    public async Task SendSubscriptionExtendedEmailAsync(string toEmail, string companyName, string planName, DateTime oldExpiryDate, DateTime newExpiryDate, int extensionDays)
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

    public async Task SendSubscriptionReactivatedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate)
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

    // ===== COMPANY CRUD EMAIL METHODS =====

    public async Task SendCompanyCreatedEmailAsync(string toEmail, string companyName, string adminName)
    {
        var subject = $"🎉 Welcome to SYNFLOX - {companyName}";
        var title = "🎉 Company Successfully Registered!";
        
        var message = $@"Welcome to SYNFLOX! Your company has been successfully registered in our system.
            <br><br>
            <strong>🏢 Company Name:</strong> {companyName}<br>
            <strong>📅 Registration Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📧 Contact Email:</strong> {toEmail}
            <br><br>
            <strong>🚀 What's next?</strong><br>
            • Set up your first subscription plan<br>
            • Configure your licensing requirements<br>
            • Explore all available features<br>
            • Contact support for any assistance<br>
            • Access your dashboard and start managing licenses
            <br><br>
            <strong>✨ SYNFLOX Features Available:</strong><br>
            • Advanced license management<br>
            • Real-time analytics and reporting<br>
            • Secure cloud-based infrastructure<br>
            • Multi-plan subscription support<br>
            • Priority technical support
            <br><br>
            Thank you for choosing SYNFLOX for your licensing needs!";
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyUpdatedEmailAsync(string toEmail, string companyName, string updatedFields)
    {
        var subject = $"📝 Company Information Updated - {companyName}";
        var title = "📝 Company Details Updated";
        
        var message = $@"Your company information has been successfully updated in SYNFLOX.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📅 Update Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📝 Updated Fields:</strong> {updatedFields}
            <br><br>
            <strong>✅ Changes Applied:</strong><br>
            • Company information has been updated<br>
            • All systems have been synchronized<br>
            • Changes are effective immediately<br>
            • Your subscriptions remain active
            <br><br>
            If you did not make these changes, please contact support immediately.";
        
        var body = BuildEmailTemplate(companyName, title, message, "#2196F3");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyActivatedEmailAsync(string toEmail, string companyName)
    {
        var subject = $"✅ Company Activated - {companyName}";
        var title = "✅ Company Successfully Activated!";
        
        var message = $@"Great news! Your company has been activated in SYNFLOX.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📅 Activation Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
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
        
        var body = BuildEmailTemplate(companyName, title, message, "#4CAF50");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyDeactivatedEmailAsync(string toEmail, string companyName, string reason)
    {
        var subject = $"⚠️ Company Deactivated - {companyName}";
        var title = "⚠️ Company Has Been Deactivated";
        
        var message = $@"Your company has been deactivated in SYNFLOX.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📅 Deactivation Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
            <strong>📝 Reason:</strong> {reason}
            <br><br>
            <strong>⚠️ What this means:</strong><br>
            • Access to SYNFLOX services is suspended<br>
            • All subscriptions are temporarily inactive<br>
            • Your data remains safe and secure<br>
            • Contact support for reactivation
            <br><br>
            Please contact our support team to resolve this matter and reactivate your company.";
        
        var body = BuildEmailTemplate(companyName, title, message, "#FF9800");
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCompanyDeletedEmailAsync(string toEmail, string companyName)
    {
        var subject = $"🗑️ Company Account Deleted - {companyName}";
        var title = "🗑️ Company Account Deleted";
        
        var message = $@"Your company account has been deleted from SYNFLOX as requested.
            <br><br>
            <strong>🏢 Company:</strong> {companyName}<br>
            <strong>📅 Deletion Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC<br>
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
        
        var body = BuildEmailTemplate(companyName, title, message, "#607D8B");
        await SendEmailAsync(toEmail, subject, body);
    }
}
