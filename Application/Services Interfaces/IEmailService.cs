using System;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Service for sending professional email notifications via Gmail SMTP
/// Comprehensive subscription lifecycle email support
/// </summary>
public interface IEmailService
{
    // Core subscription lifecycle emails
    Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, bool isTrial);
    Task SendSubscriptionActivatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate);
    Task SendSubscriptionExpiredEmailAsync(string toEmail, string companyName, string planName);
    Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string reason);
    Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate);
    Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlan, string newPlan, DateTime newExpiryDate);
    Task SendSubscriptionCanceledEmailAsync(string toEmail, string companyName, string planName);
    
    // Trial management emails
    Task SendTrialStartedEmailAsync(string toEmail, string companyName, string planName, int trialDays, DateTime expiryDate);
    Task SendTrialExpiringEmailAsync(string toEmail, string companyName, string planName, int daysRemaining);
    Task SendTrialStoppedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate);
    
    // Subscription action emails
    Task SendSubscriptionPausedEmailAsync(string toEmail, string companyName, string planName, string reason);
    Task SendSubscriptionResumedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate);
    Task SendSubscriptionExtendedEmailAsync(string toEmail, string companyName, string planName, DateTime oldExpiryDate, DateTime newExpiryDate, int extensionDays);
    Task SendSubscriptionReactivatedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate);
    Task SendAutoRenewalEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate);
    
    // Company CRUD emails
    Task SendCompanyCreatedEmailAsync(string toEmail, string companyName, string adminName);
    Task SendCompanyUpdatedEmailAsync(string toEmail, string companyName, string updatedFields);
    Task SendCompanyActivatedEmailAsync(string toEmail, string companyName);
    Task SendCompanyDeactivatedEmailAsync(string toEmail, string companyName, string reason);
    Task SendCompanyDeletedEmailAsync(string toEmail, string companyName);
}
