using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Application.Services;
using Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Sends email using the configured SMTP server. Connections use STARTTLS.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILocalizationService _localizer;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailSettings> options, 
        ILocalizationService localizer,
        ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_settings.User) || string.IsNullOrWhiteSpace(_settings.Pass))
        {
            throw new InvalidOperationException(_localizer["SmtpCredentialsMissing"]);
        }
        
        // Log the email configuration being used (for debugging)
        _logger.LogInformation(
            "Sending email - SMTP User: {User}, FromEmail: {FromEmail}, To: {To}, Host: {Host}, Port: {Port}", 
            _settings.User, _settings.FromEmail, to, _settings.Host, _settings.Port);
        
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.User, _settings.Pass),
            EnableSsl = true
        };
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.UseDefaultCredentials = false;
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body
        };
        message.To.Add(to);
        
        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully - From: {FromEmail}, To: {To}", _settings.FromEmail, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email - User: {User}, FromEmail: {FromEmail}, To: {To}", 
                _settings.User, _settings.FromEmail, to);
            throw;
        }
    }
}

/// <summary>
/// Development sender that simply writes emails to the log output.
/// </summary>
public class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("Email to {to}: {subject}\n{body}", to, subject, body);
        return Task.CompletedTask;
    }
}
