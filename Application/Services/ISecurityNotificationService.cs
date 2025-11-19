using System;
using System.Threading.Tasks;
using Application.DTOs.Security;

namespace Application.Services
{
    /// <summary>
    /// Service for sending real-time security notifications via SignalR
    /// </summary>
    public interface ISecurityNotificationService
    {
        /// <summary>
        /// Send real-time security notification to specific admin
        /// </summary>
        Task SendSecurityNotificationAsync(Guid adminId, SecurityNotificationDto notification);

        /// <summary>
        /// Send notification about 2FA being enabled
        /// </summary>
        Task Notify2FAEnabledAsync(Guid adminId, string ipAddress);

        /// <summary>
        /// Send notification about 2FA being disabled
        /// </summary>
        Task Notify2FADisabledAsync(Guid adminId, string ipAddress);

        /// <summary>
        /// Send notification about 2FA being reset
        /// </summary>
        Task Notify2FAResetAsync(Guid adminId, string ipAddress);

        /// <summary>
        /// Send notification about backup codes being generated
        /// </summary>
        Task NotifyBackupCodesGeneratedAsync(Guid adminId, int count, string ipAddress);

        /// <summary>
        /// Send notification about backup code being used
        /// </summary>
        Task NotifyBackupCodeUsedAsync(Guid adminId, int remainingCodes, string ipAddress);

        /// <summary>
        /// Send notification about backup codes running low
        /// </summary>
        Task NotifyBackupCodesLowAsync(Guid adminId, int remainingCodes);

        /// <summary>
        /// Send notification about all backup codes depleted
        /// </summary>
        Task NotifyBackupCodesDepletedAsync(Guid adminId);

        /// <summary>
        /// Send notification about failed login attempt
        /// </summary>
        Task NotifyFailedLoginAsync(Guid adminId, string ipAddress);

        /// <summary>
        /// Send notification about suspicious activity detected
        /// </summary>
        Task NotifySuspiciousActivityAsync(Guid adminId, string details);
    }
}
