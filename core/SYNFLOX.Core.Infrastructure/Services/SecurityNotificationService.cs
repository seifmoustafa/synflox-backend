using System;
using System.Threading.Tasks;
using Application.DTOs.Security;
using Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services
{
    /// <summary>
    /// Service for sending real-time security notifications via SignalR
    /// Note: Hub context is injected by WebAPI layer to avoid circular dependencies
    /// </summary>
    public class SecurityNotificationService : ISecurityNotificationService
    {
        private readonly dynamic _hubContext;

        public SecurityNotificationService(object hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendSecurityNotificationAsync(Guid adminId, SecurityNotificationDto notification)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"user_{adminId}")
                    .SendAsync("ReceiveSecurityNotification", notification);
            }
            catch
            {
                // Silent fail - real-time notifications are not critical
                // If SignalR fails, user still has email notifications
            }
        }

        public async Task Notify2FAEnabledAsync(Guid adminId, string ipAddress)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "2FA_ENABLED",
                Title = "Two-Factor Authentication Enabled",
                Message = $"2FA has been enabled from IP {ipAddress}",
                Severity = "success",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                RequiresAction = false
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task Notify2FADisabledAsync(Guid adminId, string ipAddress)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "2FA_DISABLED",
                Title = "Two-Factor Authentication Disabled",
                Message = $"2FA has been disabled from IP {ipAddress}. Your account security has been reduced.",
                Severity = "warning",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                RequiresAction = true,
                ActionUrl = "/profile/security"
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task Notify2FAResetAsync(Guid adminId, string ipAddress)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "2FA_RESET",
                Title = "Two-Factor Authentication Reset",
                Message = $"2FA has been reset from IP {ipAddress}. Please scan the new QR code.",
                Severity = "info",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                RequiresAction = true,
                ActionUrl = "/profile/security/2fa"
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task NotifyBackupCodesGeneratedAsync(Guid adminId, int count, string ipAddress)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "BACKUP_CODES_GENERATED",
                Title = "Backup Codes Generated",
                Message = $"{count} new backup codes generated from IP {ipAddress}",
                Severity = "success",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                RequiresAction = false
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task NotifyBackupCodeUsedAsync(Guid adminId, int remainingCodes, string ipAddress)
        {
            var severity = remainingCodes <= 2 ? "error" : remainingCodes <= 5 ? "warning" : "info";
            var requiresAction = remainingCodes <= 2;

            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "BACKUP_CODE_USED",
                Title = "Backup Code Used",
                Message = $"Backup code used from IP {ipAddress}. {remainingCodes} codes remaining.",
                Severity = severity,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                RequiresAction = requiresAction,
                ActionUrl = requiresAction ? "/profile/security/backup-codes" : null
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task NotifyBackupCodesLowAsync(Guid adminId, int remainingCodes)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "BACKUP_CODES_LOW",
                Title = "Backup Codes Running Low",
                Message = $"You have only {remainingCodes} backup codes remaining. Generate new codes soon.",
                Severity = "warning",
                Timestamp = DateTime.UtcNow,
                RequiresAction = true,
                ActionUrl = "/profile/security/backup-codes"
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task NotifyBackupCodesDepletedAsync(Guid adminId)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "BACKUP_CODES_DEPLETED",
                Title = "All Backup Codes Used",
                Message = "You have no backup codes remaining! Generate new codes immediately.",
                Severity = "error",
                Timestamp = DateTime.UtcNow,
                RequiresAction = true,
                ActionUrl = "/profile/security/backup-codes"
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task NotifyFailedLoginAsync(Guid adminId, string ipAddress)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "FAILED_LOGIN",
                Title = "Failed Login Attempt",
                Message = $"Failed login attempt from IP {ipAddress}",
                Severity = "warning",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                RequiresAction = false
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }

        public async Task NotifySuspiciousActivityAsync(Guid adminId, string details)
        {
            var notification = new SecurityNotificationDto
            {
                Id = Guid.NewGuid().ToString(),
                Type = "SUSPICIOUS_ACTIVITY",
                Title = "Suspicious Activity Detected",
                Message = details,
                Severity = "error",
                Timestamp = DateTime.UtcNow,
                RequiresAction = true,
                ActionUrl = "/profile/security/activity"
            };

            await SendSecurityNotificationAsync(adminId, notification);
        }
    }
}
