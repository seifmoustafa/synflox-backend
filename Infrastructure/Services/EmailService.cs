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
        
        _smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        _fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured");
        _fromName = _configuration["Email:FromName"] ?? "SYNFLOX";
        _appPassword = _configuration["Email:AppPassword"] ?? throw new InvalidOperationException("Email:AppPassword not configured");
    }

    public async Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, bool isTrial)
    {
        var subject = isTrial 
            ? _localizer["Email.TrialStarted.Subject"] 
            : _localizer["Email.SubscriptionCreated.Subject"];
        
        var body = BuildEmailTemplate(
            companyName,
            isTrial ? _localizer["Email.TrialStarted.Title"] : _localizer["Email.SubscriptionCreated.Title"],
            isTrial 
                ? string.Format(_localizer["Email.TrialStarted.Message"], planName, expiryDate.ToString("MMMM dd, yyyy"))
                : string.Format(_localizer["Email.SubscriptionCreated.Message"], planName, expiryDate.ToString("MMMM dd, yyyy")),
            "#4CAF50");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionActivatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate)
    {
        var subject = _localizer["Email.SubscriptionActivated.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.SubscriptionActivated.Title"],
            string.Format(_localizer["Email.SubscriptionActivated.Message"], planName, expiryDate.ToString("MMMM dd, yyyy")),
            "#2196F3");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionExpiredEmailAsync(string toEmail, string companyName, string planName)
    {
        var subject = _localizer["Email.SubscriptionExpired.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.SubscriptionExpired.Title"],
            string.Format(_localizer["Email.SubscriptionExpired.Message"], planName),
            "#F44336");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string reason)
    {
        var subject = _localizer["Email.SubscriptionSuspended.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.SubscriptionSuspended.Title"],
            string.Format(_localizer["Email.SubscriptionSuspended.Message"], reason),
            "#FF9800");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate)
    {
        var subject = _localizer["Email.SubscriptionRenewed.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.SubscriptionRenewed.Title"],
            string.Format(_localizer["Email.SubscriptionRenewed.Message"], planName, newExpiryDate.ToString("MMMM dd, yyyy")),
            "#4CAF50");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlan, string newPlan, DateTime newExpiryDate)
    {
        var subject = _localizer["Email.SubscriptionUpgraded.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.SubscriptionUpgraded.Title"],
            string.Format(_localizer["Email.SubscriptionUpgraded.Message"], oldPlan, newPlan, newExpiryDate.ToString("MMMM dd, yyyy")),
            "#9C27B0");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendSubscriptionCanceledEmailAsync(string toEmail, string companyName, string planName)
    {
        var subject = _localizer["Email.SubscriptionCanceled.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.SubscriptionCanceled.Title"],
            string.Format(_localizer["Email.SubscriptionCanceled.Message"], planName),
            "#607D8B");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialStartedEmailAsync(string toEmail, string companyName, string planName, int trialDays, DateTime expiryDate)
    {
        var subject = _localizer["Email.TrialStarted.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.TrialStarted.Title"],
            string.Format(_localizer["Email.TrialStarted.Message"], planName, trialDays, expiryDate.ToString("MMMM dd, yyyy")),
            "#00BCD4");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendTrialExpiringEmailAsync(string toEmail, string companyName, string planName, int daysRemaining)
    {
        var subject = _localizer["Email.TrialExpiring.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.TrialExpiring.Title"],
            string.Format(_localizer["Email.TrialExpiring.Message"], planName, daysRemaining),
            "#FF5722");

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendAutoRenewalEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate)
    {
        var subject = _localizer["Email.AutoRenewed.Subject"];
        var body = BuildEmailTemplate(
            companyName,
            _localizer["Email.AutoRenewed.Title"],
            string.Format(_localizer["Email.AutoRenewed.Message"], planName, newExpiryDate.ToString("MMMM dd, yyyy")),
            "#4CAF50");

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
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, {accentColor} 0%, {accentColor}dd 100%); padding: 40px; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 32px; font-weight: bold;'>SYNFLOX</h1>
                            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 14px;'>Central Licensing System</p>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            <h2 style='color: #333; margin: 0 0 20px 0; font-size: 24px;'>{title}</h2>
                            <p style='color: #666; line-height: 1.6; margin: 0 0 15px 0;'>Hello <strong>{companyName}</strong>,</p>
                            <p style='color: #666; line-height: 1.6; margin: 0 0 20px 0;'>{message}</p>
                            
                            <table width='100%' cellpadding='0' cellspacing='0' style='margin: 30px 0;'>
                                <tr>
                                    <td style='padding: 15px; background-color: #f9f9f9; border-left: 4px solid {accentColor}; border-radius: 4px;'>
                                        <p style='margin: 0; color: #666; font-size: 14px;'>
                                            <strong>Need help?</strong><br>
                                            Contact our support team at <a href='mailto:support@synflox.com' style='color: {accentColor}; text-decoration: none;'>support@synflox.com</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='color: #999; font-size: 12px; margin: 20px 0 0 0; line-height: 1.5;'>
                                This is an automated message from SYNFLOX Central Licensing System. Please do not reply to this email.
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f9f9f9; padding: 20px; text-align: center; border-top: 1px solid #eee;'>
                            <p style='margin: 0; color: #999; font-size: 12px;'>
                                &copy; {DateTime.UtcNow.Year} SYNFLOX. All rights reserved.
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
}
