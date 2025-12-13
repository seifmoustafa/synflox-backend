using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Common;


namespace Domain.Entities.Authentication
{
    public class RefreshToken : AuditEntity<int>
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;

        /// <summary>
        /// True if token was revoked due to security event (password change, logout, admin action)
        /// Different from IsActive (account status) and IsExpired (natural expiry)
        /// </summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// Timestamp when token was revoked
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Reason for revocation: "PasswordChanged", "Logout", "AdminAction", "SecurityEvent"
        /// Useful for security audits and forensics
        /// </summary>
        [StringLength(50)]
        public string? RevokedReason { get; set; }

        public Guid? AdminId { get; set; }

        public virtual Admin? Admin { get; set; }
    }
}
