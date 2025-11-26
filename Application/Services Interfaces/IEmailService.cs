using System;
using System.Threading.Tasks;
using Application.DTOs.Company;
using Domain.Enums;

namespace Application.Services;

/// <summary>
/// Service for sending professional email notifications via Gmail SMTP
/// Comprehensive subscription lifecycle email support
/// </summary>
public interface IEmailService
{
    // Core subscription lifecycle emails
    Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, bool isTrial, string? language = null);
    Task SendSubscriptionActivatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, string? language = null);
    Task SendSubscriptionExpiredEmailAsync(string toEmail, string companyName, string planName, string? language = null);
    Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string reason, string? language = null, string? notes = null, string? planName = null);
    Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null);
    Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlan, string newPlan, DateTime newExpiryDate, string? language = null);
    Task SendSubscriptionCanceledEmailAsync(string toEmail, string companyName, string planName, string? language = null, string? reason = null, string? notes = null);
    
    // Trial management emails
    Task SendTrialStartedEmailAsync(string toEmail, string companyName, string planName, int trialDays, DateTime expiryDate, string? language = null);
    Task SendTrialExpiringEmailAsync(string toEmail, string companyName, string planName, int daysRemaining, string? language = null);
    Task SendTrialStoppedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null);
    
    // Subscription action emails
    Task SendSubscriptionPausedEmailAsync(string toEmail, string companyName, string planName, string reason, string? language = null, string? notes = null);
    Task SendSubscriptionResumedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null);
    Task SendSubscriptionExtendedEmailAsync(string toEmail, string companyName, string planName, DateTime oldExpiryDate, DateTime newExpiryDate, int extensionDays, string? language = null, string? reason = null, string? notes = null);
    Task SendSubscriptionReactivatedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null, string? reason = null, string? notes = null);
    Task SendAutoRenewalEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? language = null);
    
    // Company CRUD emails
    Task SendCompanyCreatedEmailAsync(string toEmail, string companyName, string adminName, string? language = null);
    Task SendCompanyUpdatedEmailAsync(string toEmail, string companyName, string updatedFields, string? language = null);
    Task SendCompanyActivatedEmailAsync(string toEmail, string companyName, string? language = null);
    Task SendCompanyDeactivatedEmailAsync(string toEmail, string companyName, string reason, string? language = null);
    Task SendCompanyDeletedEmailAsync(string toEmail, string companyName, string? language = null);
    
    // Admin security emails
    Task SendEmailChangedNotificationAsync(string oldEmail, string newEmail, string adminName, string? language = null);
    Task SendPasswordChangedNotificationAsync(string toEmail, string adminName, string? language = null);
    Task SendPasswordResetOtpEmailAsync(string toEmail, string adminName, string otpCode, string magicLinkToken, int expiryMinutes, string ipAddress, string? language = null);
    Task SendPasswordResetSuccessEmailAsync(string toEmail, string adminName, string ipAddress, string? language = null);
    
    // 2FA and Backup Code security emails
    Task SendBackupCodesGeneratedEmailAsync(string toEmail, string adminName, int codesCount, string ipAddress, string? language = null);
    Task SendBackupCodeUsedEmailAsync(string toEmail, string adminName, int remainingCodes, string ipAddress, string? language = null);
    Task SendBackupCodesLowEmailAsync(string toEmail, string adminName, int remainingCodes, string? language = null);
    Task SendBackupCodesDepletedEmailAsync(string toEmail, string adminName, string? language = null);
    Task Send2FADisabledEmailAsync(string toEmail, string adminName, string ipAddress, string? language = null);
    Task Send2FAResetEmailAsync(string toEmail, string adminName, string ipAddress, string? language = null);
    
    // Custom email methods
    Task<CustomEmailResponse> SendCustomEmailAsync(CustomEmailRequest request, string? language = null);
    Task<BulkCustomEmailResponse> SendBulkCustomEmailAsync(BulkCustomEmailRequest request, string? language = null);
}
