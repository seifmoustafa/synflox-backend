using System;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Service for sending email notifications via Gmail SMTP
/// </summary>
public interface IEmailService
{
    Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, bool isTrial);
    Task SendSubscriptionActivatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate);
    Task SendSubscriptionExpiredEmailAsync(string toEmail, string companyName, string planName);
    Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string reason);
    Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate);
    Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlan, string newPlan, DateTime newExpiryDate);
    Task SendSubscriptionCanceledEmailAsync(string toEmail, string companyName, string planName);
    Task SendTrialStartedEmailAsync(string toEmail, string companyName, string planName, int trialDays, DateTime expiryDate);
    Task SendTrialExpiringEmailAsync(string toEmail, string companyName, string planName, int daysRemaining);
    Task SendAutoRenewalEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate);
}
