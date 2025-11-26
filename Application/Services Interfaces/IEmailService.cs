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
    // Subscription lifecycle emails
    Task SendSubscriptionCreatedEmailAsync(string toEmail, string companyName, string planName, DateTime expiryDate, string? language = null);
    Task SendSubscriptionRenewedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, string? reason = null, string? language = null);
    Task SendSubscriptionUpgradedEmailAsync(string toEmail, string companyName, string oldPlanName, string newPlanName, DateTime expiryDate, string? reason = null, string? language = null);
    Task SendSubscriptionExtendedEmailAsync(string toEmail, string companyName, string planName, DateTime newExpiryDate, int extensionDays, string? reason = null, string? language = null);
    Task SendSubscriptionCancelledEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null);
    Task SendSubscriptionSuspendedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null);
    Task SendSubscriptionResumedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null);
    Task SendSubscriptionPausedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null);
    Task SendSubscriptionUnpausedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null);
    Task SendSubscriptionReactivatedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null);
    Task SendTrialStoppedEmailAsync(string toEmail, string companyName, string planName, string? reason = null, string? language = null);
    
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
